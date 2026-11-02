# Appendix E — End-to-End Smoke-Test Checklist

> ARIS 3.0 · [Documentation hub](../README.md) · Companion: [§7](../07-deployment-view.md) · [§10](../10-quality-requirements.md) · [Appendix D](D-team-workflow.md)

This is the committed manual release/demo walkthrough. It mirrors
[`tests/Aris.Tests/EndToEndWorkflowTests.cs`](../../tests/Aris.Tests/EndToEndWorkflowTests.cs)
and covers the part automation cannot: WinForms navigation, dialogs, validation
messages, and the language switch.

Run the automated gates first; complete this checklist only when they are green.

---

## E.1 Preconditions

- [ ] `dotnet --version` prints `10.0.401`.
- [ ] `docker compose ps` shows `aris-sqlserver` running.
- [ ] `.\scripts\deploy_database.ps1` has completed.
- [ ] `ConnectionStrings:ArisDb` is set in user-secrets.
- [ ] `dotnet build Aris.slnx` reports 0 errors and 0 warnings.
- [ ] `ARIS_TEST_CONNECTION` is set in the test shell.
- [ ] Three accounts exist: one `ADMIN`, Officer A, Officer B.

For a clean demo, reset first — **this deletes all local data**:

```powershell
docker compose down -v
docker compose up -d
.\scripts\deploy_database.ps1