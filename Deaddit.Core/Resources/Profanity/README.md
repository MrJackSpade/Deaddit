# Profanity list

`en.json` is vendored unmodified from [dsojevic/profanity-list](https://github.com/dsojevic/profanity-list)
at commit `c27924319aa9bd6f917e3782b4f4b6604a50b652` (MIT, see `LICENSE`).

Local choices about which entries count as swearing, how loosely they match, extra exceptions and
missing words live in `Deaddit.Core/Utils/Profanity/ProfanityPolicy.cs`. To update, replace `en.json`
with a newer upstream copy; the matcher throws at load if the policy references an id that no longer exists.
