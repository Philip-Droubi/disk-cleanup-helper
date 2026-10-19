# Contributing to Disk Cleanup Helper

Thank you for helping. Bug reports, ideas, translations and code are all welcome.

## Report a bug or suggest an idea

Open an issue: https://github.com/Philip-Droubi/disk-cleanup-helper/issues. Pick "Bug report" or "Idea", and say which
version you use (About tab), your Windows version and your language. A screenshot helps a lot.

## Change the code

1. Read the [developer guide](docs/DEVELOPING.md): how to build, the folders, and the test switches.
2. Build with `build.cmd -Rebuild` (it keeps the version number; a plain `build.cmd` raises it).
3. Keep to C# 5: the compiler that ships with Windows has no `$"..."`, `?.`, `nameof`, `=>` members or tuples.
4. Match the code around your change: plain names, short comments that say why, no new libraries.
5. Safety first. Nothing may be deleted without the user selecting it and confirming, and every cleanup item explains
   what the user keeps and what they lose.
6. Check the layout of the tabs you touched with the snapshot switches, in light and dark, in English and in a
   right-to-left language (for example `/lang ar`), and at `/width 960`.
7. New source files start with the same two-line license header as the others. If you wrote the file, you may put
   your own name and year in the first line; keep the second line.
8. Open a pull request that says what changed and why, with before/after pictures for visible changes.

## Translate

Each language is one file in `src/Localization/Languages/`. Follow the translation rules in the developer guide; the
build checks placeholders, digits and units. Fixes from native speakers are especially welcome.

## License of contributions

By contributing, you agree that your contribution is licensed under the same terms as the project: the GNU General
Public License v3 with the additional terms in [NOTICE](NOTICE). You also confirm that you wrote it, or have the right
to submit it under these terms.
