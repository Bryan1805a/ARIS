# Appendix E — End-to-End Smoke-Test Checklist

> ARIS 3.9 · [Documentation hub](../README.md) · Companion: [§7](../07-deployment-view.md) · [§10](../10-quality-requirements.md) · [Appendix D](D-team-workflow.md)

This is the committed manual release/demo walkthrough. It mirrors
[`tests/Aris.Tests/EndToEndWorkflowTests.cs`](../../tests/Aris.Tests/EndToEndWorkflowTests.cs)
and covers the part automation cannot: WinForms navigation, dialogs, validation
messages, and the language switch.

Run the automated gates first; complete this checklist only when they are green.

---

## E.1 Preconditions

- [ ] `dotnet --version` prints `10.0.401` or a later `10.0.4xx` version (currently
      `10.0.403`).
- [ ] `docker compose ps` shows `aris-sqlserver` running.
- [ ] `.\scripts\deploy_database.ps1` has completed.
- [ ] `ConnectionStrings:ArisDb` is set in user-secrets.
- [ ] `dotnet build Aris.slnx` reports 0 errors and 0 warnings.
- [ ] The automated suite passes:

  ```powershell
  .\scripts\create_aris_test_connection.ps1
  dotnet test Aris.slnx          # 150 passed, 0 failed, 0 skipped
  .\scripts\delete_aris_test_connection.ps1
  ```

- [ ] `SELECT * FROM dbo.vw_GoldenInvariantViolations;` returns 0 rows.
- [ ] Three accounts exist: one `ADMIN`, Officer A, Officer B. Use **Create first
      administrator** (normal path) or the prototype-only **Developer mode** to create
      them.

For a clean demo, reset first — **this deletes all local data**:

```powershell
# Close the application before resetting.
docker compose down -v
docker compose up -d
.\scripts\deploy_database.ps1
```

---

## E.2 Authentication, bootstrap, and shell

- [ ] Start the application on an **empty** database. The login screen offers **Create
      first administrator** and shows the first-run information dialog.
- [ ] Create the first `ADMIN`. The account is active immediately and can sign in.
- [ ] Sign in with a wrong password: the message is friendly and the failure is audited
      (`Action = LOGIN`, `Result = FAILURE`).
- [ ] Sign in with the correct password: the shell opens, and the status bar shows the
      user, role, and connection state.
- [ ] As `ADMIN`, the sidebar shows Accounts, Audit Logs, and System Configuration. Sign
      out, sign in as an `OFFICER`: those three buttons are hidden, and the shell still
      works.
- [ ] **System** menu: Overview opens the dashboard; Log out returns to the login form;
      Exit closes the application.
- [ ] **Help** menu: Team introduction opens the five-member allocation dialog;
      About ARIS shows the version dialog.
- [ ] Change-password path: reset an account password as an administrator, then sign in
      with the temporary password. The forced-change dialog appears before business
      screens.

---

## E.3 Developer mode (prototype only)

- [ ] On the login screen, **Developer mode** opens the dialog with a visible
      local-prototype warning.
- [ ] Create an `OFFICER` account with a unique username and a password of at least eight
      characters. The dialog confirms success and the account can sign in immediately.
- [ ] Reusing the username, using a short password, or leaving a field blank shows a
      validation message and creates nothing.
- [ ] **Seed demo data** succeeds once (2 addresses, 6 citizens, 2 households, 1
      unassigned citizen); a second attempt reports that the data is already seeded.
- [ ] The audit log contains `CREATE_ACCOUNT` with `developerMode = true` and
      `SEED_DEMO_DATA`; neither row contains a password or hash.
- [ ] Close Developer mode; the normal login path is unaffected.

---

## E.4 Citizen management (`CIT-01`–`CIT-04`, `SRCH-01`)

- [ ] **Register citizen** with a 12-digit National ID, full name, date of birth in the
      past, and gender. The new citizen is `UNASSIGNED`.
- [ ] Validation rejects a non-12-digit ID, a future birth date, a missing name, and a
      duplicate National ID.
- [ ] Search by National ID, by name (case/accent-insensitive), and by status; the grid
      returns the expected rows.
- [ ] **View / Edit** opens the profile: demographics, household, canonical address, and
      the residency timeline.
- [ ] **Update information** saves non-identifying changes and writes an audit row with a
      before/after snapshot.
- [ ] **Deactivate citizen** on a member closes the membership and residence, sets the
      citizen `INACTIVE`, and keeps the Golden Invariant clean.
- [ ] Deactivating a head who still has members requires a successor head; deactivating a
      sole member also inactivates the household and closes its address.

---

## E.5 Household and residence (`HH-01`, `HH-02`, `HH-04`, `HH-06`, `SRCH-02`)

- [ ] **Register household** with an `UNASSIGNED` head and an active address and
      effective date. Household, address tenure, `HEAD` membership, residence, and the
      citizen's `ACTIVE` status all commit together.
- [ ] The household detail shows the current head, address, active members, and member
      and address history.
- [ ] **Add member** accepts an `UNASSIGNED` citizen and creates the matching membership
      and residence.
- [ ] Adding a citizen who already has an active membership or residence is rejected with
      a friendly message.
- [ ] **Change head** closes the old `HEAD` and the successor's `MEMBER` rows (`EndDate =
      E - 1`) and opens two new rows (`StartDate = E`). No role is updated in place.
- [ ] Search households by ID, head name, head National ID, and address text.
- [ ] Attempting to remove a member outside a transfer or deactivation is not offered.

---

## E.6 Address management (`ADR-01`–`ADR-03`, `SRCH-03`)

- [ ] **Register address** with code, address line, ward, district (optional), province,
      and `ValidFrom`; the computed address text appears.
- [ ] Search by code, street, ward, district, province, and status.
- [ ] **View address** shows the canonical successor, current households, and current
      residents.
- [ ] **Retire address** requires an active successor, a different address, and
      `ValidTo >= ValidFrom`. The old address becomes `INACTIVE` and points at its
      successor.
- [ ] Historical residences still resolve through the canonical view; `C3`/`C5`/`H3` stay
      absent and the Golden Invariant returns 0 rows.
- [ ] Registering an address from the household wizard ("New…") behaves identically.

---

## E.7 Maker-checker transfer (`UC-06`)

Use three accounts (Maker A, Checker B, and optionally a third officer).

- [ ] As Maker A, **New request** offers `JOIN_EXISTING`, `NEW_HOUSEHOLD`, and
      `HOUSEHOLD_MOVE`; the destination fields change with the type.
- [ ] Create a `JOIN_EXISTING` request for one member of a two-member household. It is
      `PENDING`; no tenure or household row has changed yet.
- [ ] As Maker A, opening the request shows that approval requires a different officer
      (`DecidedBy <> RequestedBy`); self-approval is rejected.
- [ ] As Checker B, **Open request** and approve the item. Membership and residence close
      and reopen at the destination household's address in one transaction.
- [ ] Create a second request with two moving citizens; approve one and reject the other
      with a reason. The approved citizen moves; the rejected citizen remains in the
      source household and its item shows `REJECTED`.
- [ ] Moving the last member out of a household sets the household `INACTIVE` and closes
      its active `HouseholdAddress`.
- [ ] `HOUSEHOLD_MOVE` requires every active member and keeps the same `HouseholdId`,
      changing only `HouseholdAddress` and member residences.
- [ ] `NEW_HOUSEHOLD` creates a household, assigns `NewHeadCitizenId` as `HEAD`, and opens
      the destination address.
- [ ] A request can be cancelled before decision; after a decision it cannot be edited
      (trigger `51040`).
- [ ] Every row opened by the decision carries `OpenedByRequestId`, and all audit rows for
      the decision share one `CorrelationId`.

---

## E.8 Historical rectification (`HIST-01`)

- [ ] Citizen detail → **Rectify residence** opens the shared correction dialog; the
      source row and warning are visible.
- [ ] Correct an `ENDED` residence with a reason. The source becomes `VOID`, a linked
      replacement is inserted, and `VOID_RESIDENCE` + `CORRECT_RESIDENCE` audit rows share
      one correlation ID.
- [ ] An `ACTIVE` row allows only a start-date correction; changing the address, role,
      household, or end date is refused.
- [ ] Identical values (a no-op), a double void, or an overlapping correction are
      rejected.
- [ ] A row referenced by a `PENDING` transfer item cannot be rectified until that
      request is decided or cancelled.
- [ ] Household detail → **Rectify member** and **Rectify address** follow the same rules,
      and the Golden Invariant stays at 0 rows.

---

## E.9 Temporary residence (`TEMP-01`–`TEMP-03`)

- [ ] **Register** a temporary stay: look up the citizen by CCCD, choose an active address,
      enter start/end dates, stay reason, and host information.
- [ ] A duration longer than two years, an end date before the start date, a missing
      reason, or a missing citizen is rejected.
- [ ] Search by National ID, citizen name, and status (`ACTIVE`, `EXPIRED`, `CANCELLED`).
- [ ] Edit an active record to extend the end date or update the reason/host/notes; the
      change is audited.
- [ ] Cancel the record when the citizen departs; it becomes `CANCELLED` and cannot be
      extended.
- [ ] A record whose end date has passed is displayed as `EXPIRED` without rewriting the
      stored row.

---

## E.10 Administration, audit, configuration, and reports

- [ ] **Accounts**: create an account (forced password change on first login), lock and
      unlock it, reset its password, and disable/activate it.
- [ ] Each account action writes a `SUCCESS` audit row with the acting administrator and
      safe details; no password or hash appears.
- [ ] **Audit Logs**: filter by from/to date, actor or attempted username, entity, action,
      and result. Rows are newest first and the detail pane shows IDs, correlation ID, and
      JSON details read-only.
- [ ] An `OFFICER` cannot open the audit viewer from the UI, and a direct service call
      returns an authorization failure.
- [ ] **System Configuration**: edit a key such as `LOCKOUT_MINUTES`, save, and verify the
      audit row and the new value on reload.
- [ ] **Reports**: generate Residence Statistics, Household Statistics, and a Residence
      Movement Report for a date range; export the grid to CSV.
- [ ] A report with `From > To` shows a validation message instead of an exception.

---

## E.11 Language switch

- [ ] The login screen starts in Vietnamese; the **English / Tiếng Việt** button switches
      labels and the button text immediately.
- [ ] In the shell, the status-bar language button switches static labels, buttons, form
      titles, and grid headers.
- [ ] Domain/service validation messages remain English in both modes (documented
      limitation).
- [ ] Switching language does not change data, the current screen, or the signed-in user.

---

## E.12 Final checks

- [ ] `SELECT * FROM dbo.vw_GoldenInvariantViolations;` returns **0 rows**.
- [ ] The audit trail contains rows for every action performed above, with correlation
      IDs and no credentials.
- [ ] `dotnet build Aris.slnx` → 0 errors, 0 warnings.
- [ ] `dotnet test Aris.slnx` → 150 passed with the local database.
- [ ] `git status` shows no `.env`, secret, `bin/`, `obj/`, or `TestResults/` content
      staged.
- [ ] The Developer mode warning is visible and the feature is still confined to the
      login screen (`D21`, `R21`, `T15`).
