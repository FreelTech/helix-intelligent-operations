# Lessons learned

One line per lesson, dated, with the stage it came from. Write these as you hit them,
not at the end.

| Date | Stage | Lesson |
|---|---|---|
| 2026-08-17 | 0 | Deleting a file in a later commit does not remove it from Git history. `.gitignore` must exist before the first commit, not after the first mistake. |
| 2026-08-17 | 0 | A budget alert notifies; only a spending limit blocks. Converting to pay-as-you-go removes the only hard stop, permanently. |
| 2026-08-17 | 0 | Requiring pull-request approvals as a sole developer makes the default branch unmergeable, and GitHub gives no warning when you save the rule. |
| 2026-08-17 | 0 | A required status check whose workflow never runs does not fail — it hangs on "Waiting for status to be reported". Build the check, watch it pass, then require it. |
| 2026-08-17 | 0 | CI needs only a repository and something worth checking; CD needs environments and an identity. Conflating them is how automation gets postponed for three months. |
| 2026-08-17 | 0 | Leaving any "initialise this repository" option ticked on GitHub creates a commit your local history lacks, and the first push is rejected as non-fast-forward. Force-pushing over a machine-generated placeholder is fine; the same command against a repo anyone has cloned destroys work. Use `--force-with-lease`, never `--force`. |
| 2026-08-17 | 0 | Squash merge rewrites the change into a new commit, so `git branch -d` always refuses afterwards. `-D` is correct — but a safety that fires on every PR trains you to ignore it, so verify the content is on `main` before deleting, every time. |