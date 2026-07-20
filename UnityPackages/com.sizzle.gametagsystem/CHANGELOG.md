# Changelog

## 0.1.2 - 2026-07-20

### Changed (Optimization)
- Removed GC allocations from `GameTagContainer` event listener dispatch loop (Dirty flag caching applied).
- Removed LINQ closure and enumerator allocations in `HasExactTagsAll` and `HasExactTagsAny`.
- Replaced closure allocations in `AddTagTimed` with struct references (`TimedTagHandle`).
- Improved dictionary lookup overhead in `AddTag` and `RemoveTag` from 3 to 2 queries.
- Optimized tag iteration deletion in `Tick` using $O(1)$ Swap-Back technique.
- Replaced internal `lock` in `GameTag` with `ConcurrentDictionary` to make it thread-safe and lock-free (Avoids JobSystem/Multithreading bottlenecks).
- Added `ClearCache` method to `GameTag` to prevent memory leaks from dynamically generated strings.
- Reduced GC allocations when invoking `GetTimedTags()` by providing a Fill-List pattern overload.
## 0.1.1 - 2026-05-24

- Added timed-tag snapshot accessors for editor tooling and debugging windows.
- Enabled external tools such as Ability Debugger to display remaining timed-tag durations without touching internal runtime state.
