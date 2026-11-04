"""
End-to-end workflow test for the AOC MDM web app (stdlib only).

Each phase signs in through the login page as a demo user (db/postgres/005_demo_users.sql),
so one running app instance covers every role. Start the app, then:
    python tests/e2e_workflow.py smoke        # admin1: every page, filters, search, layout, unmapped data
    python tests/e2e_workflow.py access       # login, wrong password, what each role can / cannot open, user admin
    python tests/e2e_workflow.py admin        # admin1: direct edit, enable/disable
    python tests/e2e_workflow.py maker        # maker1: submit vendor changes for approval
    python tests/e2e_workflow.py checker      # checker1: approve one request (with comment), reject the other
    python tests/e2e_workflow.py hierarchy    # admin1: rename cascade, duplicate check, cascade disable, delete, history
Or run everything on fresh data:  tests/run_e2e.ps1
"""
import http.cookiejar
import os
import re
import subprocess
import sys
import urllib.parse
import urllib.request
from html.parser import HTMLParser

BASE = os.environ.get("MDM_URL", "http://localhost:5081")
PSQL = os.environ.get("PSQL", r"C:\Program Files\PostgreSQL\18\bin\psql.exe")
PGENV = dict(os.environ)  # psql reads PGPASSWORD from the environment

jar = http.cookiejar.CookieJar()


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k):
        return None


opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar), NoRedirect)
failures = 0


def check(ok, msg):
    global failures
    print(f"  [{'PASS' if ok else 'FAIL'}] {msg}")
    failures += 0 if ok else 1


def request(path, data=None):
    body = urllib.parse.urlencode(data).encode() if data is not None else None
    try:
        with opener.open(urllib.request.Request(BASE + path, data=body)) as r:
            return r.status, r.read().decode("utf-8", "replace"), r.headers.get("Location")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace"), e.headers.get("Location")


DEMO_PASSWORD = os.environ.get("MDM_DEMO_PASSWORD", "Demo@123")


def login(user, password=DEMO_PASSWORD):
    """Sign in through the login page (clears the previous session first)."""
    jar.clear()
    status, html, _ = request("/Account/Login")
    token = form(html, "/Account/Login").get("__RequestVerificationToken", "")
    status, html, location = request("/Account/Login", {"login": user, "password": password, "__RequestVerificationToken": token})
    return status == 302, html


def sql(query):
    out = subprocess.run([PSQL, "-h", "localhost", "-U", "postgres", "-d", "aoc_mdm", "-At", "-F", "|", "-c", query],
                         capture_output=True, text=True, env=PGENV, check=True)
    return [line.split("|") for line in out.stdout.strip().splitlines() if line]


class FormParser(HTMLParser):
    """Collects the fields of the first <form> whose action contains `action`."""

    def __init__(self, action):
        super().__init__()
        self.action, self.in_form, self.fields = action, False, {}
        self._select = self._textarea = self._option_text = None

    def handle_starttag(self, tag, attrs):
        a = dict(attrs)
        if tag == "form" and self.action.lower() in (a.get("action") or "").lower():
            self.in_form = True
        if not self.in_form:
            return
        if tag == "input" and a.get("name") and a.get("type") not in ("submit", "button", "checkbox"):
            self.fields[a["name"]] = a.get("value") or ""
        elif tag == "select" and a.get("name"):
            self._select = a["name"]
            self.fields.setdefault(self._select, "")
        elif tag == "option" and self._select and "selected" in a:
            # like a browser: an <option> without a value attribute submits its text
            if "value" in a:
                self.fields[self._select] = a["value"] or ""
            else:
                self._option_text = self._select
        elif tag == "textarea" and a.get("name"):
            self._textarea = a["name"]
            self.fields[self._textarea] = ""

    def handle_data(self, data):
        if self._textarea:
            self.fields[self._textarea] += data
        elif self._option_text:
            self.fields[self._option_text] = data.strip()
            self._option_text = None

    def handle_endtag(self, tag):
        if tag == "select":
            self._select = None
        elif tag == "textarea":
            self._textarea = None
        elif tag == "form" and self.in_form:
            self.in_form = False
            self.action = "\0"  # only the first matching form


def form(html, action):
    p = FormParser(action)
    p.feed(html)
    return p.fields


def vendor_edit(vendor_id, changes):
    status, html, _ = request(f"/VendorRefAttri/Edit?id={vendor_id}&Source=sql_db&VendorName=x&VendorNumber=x&SpendArea=x")
    check(status == 200, f"GET vendor edit page {vendor_id} -> {status}")
    fields = form(html, "/VendorRefAttri/Edit")
    fields.update(changes)
    status, _, location = request(f"/VendorRefAttri/Edit?id={vendor_id}", fields)
    check(status == 302 and "Error" not in (location or ""), f"POST vendor edit {vendor_id} -> {status} {location}")


def vendor_row(vendor_id):
    return sql(f"SELECT parent_company, indirect_spend_owner, updatestatus, lastmodifiedbyrole, updatedby "
               f"FROM mdm.vendor_reference_attributes WHERE vendor_id = '{vendor_id}'")[0]


# ---------------------------------------------------------------------------------------------
def smoke():
    print("== Smoke: every page")
    for p in ["/", "/SpendArea/Index", "/SpendArea/BulkUpdate", "/Team/Index", "/Team/BulkUpdate",
              "/AOCLevel1/Index", "/AOCLevel1/BulkUpdate", "/AOCLevel2/Index", "/AOCLevel2/BulkUpdate",
              "/AOCLevel3/Index", "/AOCLevel3/BulkUpdate", "/VendorRefAttri/Index", "/VendorRefAttri/BulkUpdate",
              "/VendorRefAttri/VendorAudit", "/ItemRefAttrs/Index", "/ItemRefAttrs/BulkUpdate", "/ItemRefAttrs/ItemAudit",
              "/ItemSupplierRefAttri/Index", "/ItemSupplierRefAttri/BulkUpdate", "/ItemSupplierRefAttri/ItemSupplierAudit"]:
        status, _, _ = request(p)
        check(status == 200, f"GET {p} -> {status}")
    status, html, _ = request("/VendorRefAttri/Index?pageNumber=1&pageSize=25&filterColumnName=vendor_english_name&filterValue=acme")
    check(status == 200 and "Acme Chemical" in html and "Global Resins" not in html, "Vendor filter vendor_english_name ILIKE 'acme'")
    status, html, _ = request("/VendorRefAttri/BulkUpdate?pageNumber=1&pageSize=25&search=pumpworks")
    check(status == 200 and "PumpWorks" in html and "Acme Chemical" not in html, "Vendor global search 'pumpworks'")

    print("== BLITZDG layout")
    status, html, _ = request("/AOCLevel1/Index")
    check("Vendor Hierarchy" in html and "Master Data" in html, "Navbar has 'Vendor Hierarchy' and 'Master Data' menus")
    check("Vendor Level 1" in html and "AOC Level" not in html, "Page title 'Vendor Level 1'")
    check("itconvergence-logo.png" in html and "IT Convergence MDM MetaData" in html, "IT Convergence branding kept")
    status, html, _ = request("/VendorRefAttri/Index")
    check(re.search(r"\d{2}-\d{2}-\d{4} \d{2}:\d{2}:\d{2}", html) is not None, "Updated Date shown as dd-MM-yyyy HH:mm:ss")

    print("== Unmapped data (no Level 3)")
    status, html, _ = request("/VendorRefAttri/Index?pageNumber=1&pageSize=25&filterColumnName=level_3&filterValue=__unmapped__")
    check(status == 200 and "Unmapped Vendors" in html and "NewCo Supplies" in html and "Acme Chemical" not in html,
          "Unmapped Vendors lists only vendors without Level 3")
    status, html, _ = request("/ItemRefAttrs/Index?pageNumber=1&pageSize=25&filterColumnName=level_3&filterValue=__unmapped__")
    check(status == 200 and "Unmapped Items" in html and "Solvent Blend" in html and "Phthalic Anhydride" not in html,
          "Unmapped Items lists only items without Level 3")


def admin():
    print("== Admin (level 4): direct update, no approval")
    vendor_edit("V1005", {"IndirectSpendOwner": "CIO Office"})
    row = vendor_row("V1005")
    check(row[1] == "CIO Office" and row[2] == "UpdateDirectly", f"V1005 updated directly -> {row}")
    level3 = sql("SELECT level_3 FROM mdm.vendor_reference_attributes WHERE vendor_id = 'V1005'")[0][0]
    check(level3 == "ERP Consulting", f"V1005 keeps its Level 3 after edit ({level3})")

    print("== Admin: Spend Area enable/disable toggle")
    before = sql("SELECT enabled FROM mdm.spend_area WHERE spend_area = 'Logistics'")[0][0]
    status, _, _ = request("/SpendArea/Enable?id=Logistics")
    after = sql("SELECT enabled FROM mdm.spend_area WHERE spend_area = 'Logistics'")[0][0]
    check(status == 302 and before != after, f"Logistics enabled {before} -> {after}")
    request("/SpendArea/Enable?id=Logistics")  # restore


def maker():
    print("== Maker (level 2): changes go to the approval queue")
    before = vendor_row("V1002")
    vendor_edit("V1002", {"ParentCompany": "Global Group APPROVE-ME"})
    vendor_edit("V1003", {"ParentCompany": "PackRight REJECT-ME"})
    row = vendor_row("V1002")
    check(row[0] == before[0], f"V1002 main value unchanged until approved ({row[0]})")
    check(row[2] == "PendingApproval", f"V1002 flagged PendingApproval ({row[2]})")
    pending = sql("SELECT vendor_id, parent_company, makerid FROM mdm.vendor_reference_attributes_audit "
                  "WHERE approvalstatus = 'PendingApproval' AND vendor_id IN ('V1002','V1003') ORDER BY auditid")
    check(len(pending) >= 2, f"Audit rows pending: {pending}")


def checker():
    print("== Checker (level 3): approve one, reject one")
    status, html, _ = request("/VendorRefAttri/VendorAudit")
    check(status == 200 and "APPROVE-ME" in html and "REJECT-ME" in html, "Both requests listed on Vendor Approve page")

    approve_id = sql("SELECT max(auditid) FROM mdm.vendor_reference_attributes_audit "
                     "WHERE vendor_id='V1002' AND approvalstatus='PendingApproval'")[0][0]
    reject_id = sql("SELECT max(auditid) FROM mdm.vendor_reference_attributes_audit "
                    "WHERE vendor_id='V1003' AND approvalstatus='PendingApproval'")[0][0]
    rejected_before = vendor_row("V1003")[0]

    status, html, _ = request("/VendorRefAttri/Approve?id=V1002&source=sql_db")
    check(status == 200 and "APPROVE-ME" in html, "Approve page shows current vs proposed values")

    token = form(html, "/VendorRefAttri/Reject").get("__RequestVerificationToken", "")
    status, _, loc = request(f"/VendorRefAttri/Approved?id={approve_id}",
                             {"auditId": approve_id, "Remarks": "e2e approved with comment", "__RequestVerificationToken": token})
    check(status == 302, f"Approve audit {approve_id} (with review comment) -> {status} {loc}")
    row = vendor_row("V1002")
    check(row[0] == "Global Group APPROVE-ME" and row[2] == "Approved", f"V1002 approved value applied -> {row}")
    remark = sql(f"SELECT remarks FROM mdm.vendor_reference_attributes_audit WHERE auditid={approve_id}")[0][0]
    check(remark == "e2e approved with comment", f"Approval review comment saved ('{remark}')")
    level3 = sql("SELECT level_3 FROM mdm.vendor_reference_attributes WHERE vendor_id = 'V1002'")[0][0]
    check(level3 == "Isophthalic UPR", f"V1002 keeps its Level 3 after approval ({level3})")

    status, html, _ = request(f"/VendorRefAttri/Reject?id={reject_id}")
    token = form(html, "/VendorRefAttri/Reject").get("__RequestVerificationToken", "")
    status, _, loc = request("/VendorRefAttri/Reject",
                             {"auditId": reject_id, "Remarks": "e2e test rejection", "__RequestVerificationToken": token})
    check(status == 302, f"Reject audit {reject_id} -> {status} {loc}")
    audit = sql(f"SELECT approvalstatus, checkerid, remarks FROM mdm.vendor_reference_attributes_audit WHERE auditid={reject_id}")[0]
    check(audit[0] == "Rejected", f"Audit {reject_id} marked Rejected by {audit[1]} ({audit[2]})")
    row = vendor_row("V1003")
    check(row[0] == rejected_before, f"V1003 keeps original value after reject: '{row[0]}' (was '{rejected_before}')")


def denied(path):
    status, _, location = request(path)
    return status == 302 and "/Error/AccessDenied" in (location or "")


def access():
    print("== Login and access per role")
    jar.clear()
    status, _, location = request("/SpendArea/Index")
    check(status == 302 and "/Account/Login" in (location or ""), f"Not signed in -> redirected to login ({location})")
    ok, html = login("maker1", "wrong-password")
    check(not ok and "Invalid user name or password" in html, "Wrong password is rejected")

    ok, _ = login("viewer1")
    check(ok, "viewer1 signs in")
    status, html, _ = request("/VendorRefAttri/Index")
    check(status == 200 and "Vera Viewer" in html and "Approvals" not in html, "Viewer sees data, name in header, no Approvals menu")
    check(denied("/VendorRefAttri/Edit?id=V1001&Source=sql_db"), "Viewer cannot open vendor Edit")
    check(denied("/AOCLevel1/Edit?id=Resins"), "Viewer cannot edit the hierarchy")

    ok, _ = login("maker1")
    status, _, _ = request("/VendorRefAttri/Edit?id=V1001&Source=sql_db&VendorName=x&VendorNumber=x&SpendArea=x")
    check(ok and status == 200, "Maker can open vendor Edit")
    check(denied("/VendorRefAttri/VendorAudit"), "Maker cannot open Vendor Approval")
    check(denied("/Users/Index"), "Maker cannot manage users")

    ok, _ = login("checker1")
    status, html, _ = request("/VendorRefAttri/VendorAudit")
    check(ok and status == 200, "Checker can open Vendor Approval")
    check(denied("/Users/Index"), "Checker cannot manage users")

    ok, _ = login("admin1")
    status, html, _ = request("/Users/Index")
    check(ok and status == 200 and all(u in html for u in ["viewer1", "maker1", "maker2", "checker1", "admin1"]),
          "Admin sees the user list with all demo users")
    status, html, _ = request("/Users/Edit")
    token = form(html, "/Users/Edit").get("__RequestVerificationToken", "")
    request("/Users/Edit", {"IsNew": "true", "Login": "e2e_user", "DisplayName": "E2E User", "LevelNo": "2",
                            "Password": "E2ePass#1", "IsActive": "true", "__RequestVerificationToken": token})
    ok, _ = login("e2e_user", "E2ePass#1")
    check(ok and not denied("/VendorRefAttri/Edit?id=V1001&Source=sql_db&VendorName=x&VendorNumber=x&SpendArea=x"),
          "Admin-created user signs in with Maker access")
    sql_exec("DELETE FROM intranet.user_access WHERE login = 'e2e_user'")


def hierarchy():
    print("== Hierarchy: rename, integrity, cascade, delete, history")
    login("admin1")
    status, html, _ = request("/Hierarchy/Manage?level=level2&id=Unsaturated%20Polyester")
    check(status == 200 and "Level 3 below" in html, "Manage page shows what is attached")

    token = form(html, "/Hierarchy/Rename").get("__RequestVerificationToken", "")
    status, _, _ = request("/Hierarchy/Rename", {"level": "level2", "id": "Unsaturated Polyester", "newName": "UP Resins",
                                                 "__RequestVerificationToken": token})
    l3 = sql("SELECT count(*) FROM mdm.aoc_level_3 WHERE level_2 = 'UP Resins'")[0][0]
    v = sql("SELECT count(*) FROM mdm.vw_vendor_reference_attributes WHERE mdm_level_2 = 'UP Resins'")[0][0]
    check(status == 302 and l3 == "2" and v == "2", f"Rename cascades to {l3} Level 3 rows and {v} vendors")

    status, html, _ = request("/Hierarchy/Manage?level=level2&id=Vinyl%20Ester")
    token = form(html, "/Hierarchy/Rename").get("__RequestVerificationToken", "")
    request("/Hierarchy/Rename", {"level": "level2", "id": "Vinyl Ester", "newName": "up resins", "__RequestVerificationToken": token})
    still = sql("SELECT count(*) FROM mdm.aoc_level_2 WHERE level_2 = 'Vinyl Ester'")[0][0]
    check(still == "1", "Duplicate name (different case) is refused")

    request("/Team/Enable?id=Chemicals%20Team")   # disable
    off = sql("SELECT count(*) FROM mdm.vw_aoc_level_3 WHERE team = 'Chemicals Team' AND enabled = 'No'")[0][0]
    check(off == "4", f"Disabling a Team disables the {off} Level 3 rows below it")
    status, _, _ = request("/AOCLevel1/Enable?id=Resins")
    resins = sql("SELECT enabled FROM mdm.aoc_level_1 WHERE level_1 = 'Resins'")[0][0]
    check(resins == "No", "Cannot enable a Level 1 while its Team is disabled")
    request("/Team/Enable?id=Chemicals%20Team")   # re-enable the team (children stay disabled until enabled one by one)

    status, html, _ = request("/Hierarchy/Manage?level=level3&id=Pump%20Parts")
    check("Cannot be deleted" in html, "In-use Level 3 cannot be deleted")
    sql_exec("INSERT INTO mdm.aoc_level_3 (level_3, level_2, enabled, isactive) VALUES ('E2E Temp L3', 'Spare Parts', 'Yes', true)")
    status, html, _ = request("/Hierarchy/Manage?level=level3&id=E2E%20Temp%20L3")
    token = form(html, "/Hierarchy/Delete").get("__RequestVerificationToken", "")
    request("/Hierarchy/Delete", {"level": "level3", "id": "E2E Temp L3", "__RequestVerificationToken": token})
    gone = sql("SELECT count(*) FROM mdm.aoc_level_3 WHERE level_3 = 'E2E Temp L3'")[0][0]
    check(gone == "0", "Unused Level 3 is deleted")

    status, html, _ = request("/Hierarchy/History")
    check(status == 200 and "RENAME" in html and "DELETE" in html and "admin1" in html,
          "History shows the rename and delete by admin1")


def sql_exec(query):
    subprocess.run([PSQL, "-h", "localhost", "-U", "postgres", "-d", "aoc_mdm", "-q", "-c", query],
                   capture_output=True, text=True, env=PGENV, check=True)


if __name__ == "__main__":
    phase = sys.argv[1]
    user = {"smoke": "admin1", "admin": "admin1", "maker": "maker1", "checker": "checker1"}.get(phase)
    if user:
        signed_in, _ = login(user)
        check(signed_in, f"signed in as {user}")
    {"smoke": smoke, "admin": admin, "maker": maker, "checker": checker, "access": access, "hierarchy": hierarchy}[phase]()
    print(f"\n{'ALL PASSED' if failures == 0 else f'{failures} FAILURE(S)'}")
    sys.exit(1 if failures else 0)
