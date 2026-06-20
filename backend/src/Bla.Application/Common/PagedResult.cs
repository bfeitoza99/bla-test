namespace Bla.Application.Common;

/// <summary>
/// A single page of a larger result set: the items on this page plus the paging metadata needed to
/// render navigation (<see cref="Page"/>, <see cref="PageSize"/>, and the total count across all
/// pages). Serializes to <c>{ items, page, pageSize, total }</c> per the Tasks API contract.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);
