# Hindi navigation labels

Scope: visible navigation labels in the nine shared layout views only.
Routes, controller/action names, permissions, database logic and billing work are unchanged.

`hindi-navigation-rollback.json` contains exact Before and After text for each layout.
Do not rerun `translate-navigation.ps1` or overwrite this snapshot.

When the user requests rollback:

1. Compare each current layout with its saved After text.
2. If identical, restore its Before text.
3. If it has later edits, reverse only the Before-to-After label changes using a
   contextual patch; preserve those later edits and resolve conflicts individually.
4. Run `git diff --check` and build. Never reset the whole worktree.

This record does not authorize rollback until the user asks for it.
