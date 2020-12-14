# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2020-12-13
### Added
- Added `Flatten` methods to `NativeArray2D` to create a one-dimensional NativeArray or managed array and copy all elements into the new array.
## Changed
- Updated to Burst.1.4.3

## [0.1.5] - 2020-12-03
### Changed
- Updated to com.unity.burst-1.4.2 
- Updated README
### Fixed
- Fixed missing com.unity.jobs dependency.
- Fixed some bugs for NativeArray2D.

## [0.1.4] - 2020-11-20
### Changed
- Removed Unity.Mathematics dependency

## [0.1.3] - 2020-11-1
### Added
- Updated dependencies
- Fixed tests

## [0.1.2] - 2020-08-23
### Added
- Added a changelog
- Made NativePtr compatible with DeallocateOnJobCompletion.