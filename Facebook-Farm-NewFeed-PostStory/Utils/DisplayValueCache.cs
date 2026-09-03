using System;
using System.Collections.Generic;
using System.Threading;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// Bounded, account-keyed display-string cache for the VirtualMode account grid.
    ///
    /// Goal: eliminate repeated allocations in the paint/render hot path —
    ///   • repeated <see cref="Guid.ToString()"/> for unchanged Ids,
    ///   • repeated string truncation / formatting of unchanged cell values,
    /// by reusing a previously formatted display string whenever the underlying
    /// raw value (identified by its hash) has not changed.
    ///
    /// Lookup key = (Account Id, Column Index, Raw Value Hash):
    ///   • Account Id  → outer dictionary slot (one per row/account),
    ///   • Column Index → array slot inside the account's slot,
    ///   • Raw Value Hash → guard stored alongside the cached string. A changed
    ///     value produces a different hash → miss → recompute, so stale strings
    ///     are self-correcting and never returned.
    ///
    /// Bounding / eviction: a two-segment generational (second-chance) scheme.
    /// New / freshly-used accounts live in <c>_hot</c>; when <c>_hot</c> fills,
    /// the whole <c>_cold</c> segment is evicted and <c>_hot</c> becomes the new
    /// <c>_cold</c>. This caps resident accounts at ~2·<see cref="SegmentCapacity"/>
    /// (≈ 2·1024) regardless of how many distinct accounts scroll past, so memory
    /// can never grow unbounded. Eviction is O(1) amortized and allocation-free on hits.
    ///
    /// Threading: all structural mutation (TryGet/Put/InvalidateAccount/Clear) is
    /// performed on the UI thread (same contract as AccountWindowCache). The 30s
    /// metrics timer only reads Interlocked counters and the (int) segment counts,
    /// so no lock is taken in the per-cell hot path.
    /// </summary>
    public sealed class DisplayValueCache
    {
        // ~1024 accounts per generation → ≤ ~2048 resident accounts.
        private const int SegmentCapacity = 1024;
        // Default per-account column slots; grows on demand if a grid has more columns.
        private const int InitialColumnCapacity = 48;

        /// <summary>Per-account formatted-value slots, indexed by column.</summary>
        private sealed class Slot
        {
            public int[] Hashes;
            public string?[] Values;

            public Slot(int columnCapacity)
            {
                Hashes = new int[columnCapacity];
                Values = new string?[columnCapacity];
            }

            public void EnsureColumn(int columnIndex)
            {
                if (columnIndex < Values.Length) return;
                int newLen = Values.Length;
                while (newLen <= columnIndex) newLen *= 2;
                Array.Resize(ref Hashes, newLen);
                Array.Resize(ref Values, newLen);
            }
        }

        private Dictionary<Guid, Slot> _hot = new(SegmentCapacity);
        private Dictionary<Guid, Slot> _cold = new(SegmentCapacity);

        private long _hits;
        private long _misses;
        private long _evictions;

        /// <summary>Approximate number of resident accounts (diagnostic only).</summary>
        public int Count => _hot.Count + _cold.Count;

        /// <summary>
        /// O(1) lookup. Returns the cached display string when the raw value (by hash)
        /// is unchanged for this (account, column); otherwise null. No allocations on hit.
        /// </summary>
        public string? TryGet(Guid accountId, int columnIndex, int rawHash)
        {
            if (_hot.TryGetValue(accountId, out var slot))
            {
                string? v = Read(slot, columnIndex, rawHash);
                if (v != null) { Interlocked.Increment(ref _hits); return v; }
                Interlocked.Increment(ref _misses);
                return null;
            }

            if (_cold.TryGetValue(accountId, out slot))
            {
                string? v = Read(slot, columnIndex, rawHash);
                // Promote the slot to the hot segment regardless of cell hit/miss:
                // the account is clearly in active use. Reuses the existing Slot,
                // so no allocation.
                Promote(accountId, slot);
                if (v != null) { Interlocked.Increment(ref _hits); return v; }
                Interlocked.Increment(ref _misses);
                return null;
            }

            Interlocked.Increment(ref _misses);
            return null;
        }

        /// <summary>Store a freshly formatted display string for (account, column, hash).</summary>
        public void Put(Guid accountId, int columnIndex, int rawHash, string formatted)
        {
            if (columnIndex < 0) return;

            if (_hot.TryGetValue(accountId, out var slot))
            {
                slot.EnsureColumn(columnIndex);
                slot.Hashes[columnIndex] = rawHash;
                slot.Values[columnIndex] = formatted;
                return;
            }

            if (_cold.TryGetValue(accountId, out slot))
            {
                slot.EnsureColumn(columnIndex);
                slot.Hashes[columnIndex] = rawHash;
                slot.Values[columnIndex] = formatted;
                Promote(accountId, slot);
                return;
            }

            // New account → goes into hot, may trigger a generational rotation.
            slot = new Slot(Math.Max(InitialColumnCapacity, columnIndex + 1));
            slot.Hashes[columnIndex] = rawHash;
            slot.Values[columnIndex] = formatted;
            AddHot(accountId, slot);
        }

        /// <summary>
        /// Drop all cached cells for one account (called when its data changes).
        /// O(1) — a single dictionary removal per segment.
        /// </summary>
        public void InvalidateAccount(Guid accountId)
        {
            _hot.Remove(accountId);
            _cold.Remove(accountId);
        }

        /// <summary>Drop everything (called on page eviction / scope reset).</summary>
        public void Clear()
        {
            _hot.Clear();
            _cold.Clear();
        }

        /// <summary>Read+reset hit/miss/eviction counters for periodic metrics reporting.</summary>
        public (long hits, long misses, long evictions) TakeSnapshot()
        {
            long h = Interlocked.Exchange(ref _hits, 0);
            long m = Interlocked.Exchange(ref _misses, 0);
            long e = Interlocked.Exchange(ref _evictions, 0);
            return (h, m, e);
        }

        // ── internals ──────────────────────────────────────────────────────

        private static string? Read(Slot slot, int columnIndex, int rawHash)
        {
            if ((uint)columnIndex >= (uint)slot.Values.Length) return null;
            return slot.Hashes[columnIndex] == rawHash ? slot.Values[columnIndex] : null;
        }

        private void Promote(Guid accountId, Slot slot)
        {
            _cold.Remove(accountId);
            AddHot(accountId, slot);
        }

        private void AddHot(Guid accountId, Slot slot)
        {
            _hot[accountId] = slot;
            if (_hot.Count <= SegmentCapacity) return;

            // Rotate generations: evict the entire cold segment, hot becomes cold.
            if (_cold.Count > 0)
                Interlocked.Add(ref _evictions, _cold.Count);
            var oldCold = _cold;
            oldCold.Clear();      // reuse the dictionary backing store
            _cold = _hot;
            _hot = oldCold;
        }
    }
}
