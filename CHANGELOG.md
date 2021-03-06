# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.6.0] - 2021-07-10
### Changed
 - Updated package dependencies
 - Changed minimum Unity version to 2020.3.0f1.
### Removed
 - Removed NativePtr because apparently Unity added NativeReference to Collections in 0.11.0-preview.17.
 - Removed NativeHeap<T> in favor of NativeHeap<TValue, TPriority>.

## [0.5.0] - 2021-04-09
### Added
- Added NativeArray3D native collection
- Added NativeHeap.IsCreated property
### Fixed
- Fixed NativeHeap.PushPop and Replace
- Fixed NativePtr.Dispose not setting the ptr to null on dispose.

## [0.4.2] - 2021-01-22
### Changed
- Changed NativeStack into proper namespace
### Fixed
- Fixed unimplemented NativePtr.Dispose(JobHandle)
- Fixed tests

## [0.4.1] - 2021-01-21
### Fixed
- Fixed test

## [0.4.0] - 2021-01-21
### Changed
- Updated Burst from 1.4.3 to 1.4.4

## [0.3.0] - 2021-01-01
### Changed
- Changed NativeHeap<TValue, TPriority> to not use value tuples because of issues with burst. Replaced with HeapNode<TValue, TPriority> structs.
### Fixed
- Fixed some tests

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