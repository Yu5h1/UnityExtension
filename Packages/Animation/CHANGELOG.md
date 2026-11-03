# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Humanoid importer (Editor): `HumanoidImportSettings` generates a humanoid Avatar from a character GLB and humanoid clips with root motion from a motion GLB, with ModelImporter-style root transform settings. See README.
- Control intervals: the humanoid importer reads `animations[].extras.yu5h1.intervals` of a motion GLB into a runtime `MotionIntervals` sub-asset per clip (ANIMATION-UNITYEXTENSION-1). A GLB without intervals imports as before.

### Changed

- Humanoid bone map: `humanoid.bones.json` synced with the Character Builder skeleton table; the hand-held `prop_l` and `prop_r` anchors are no longer listed in `unmapped`.

## [0.1.0] - 2026-04-21

### This is the first release of *\<Yu5h1Lib.Animation\>*.

*Short description of this release*
