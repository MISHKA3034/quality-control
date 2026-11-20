import { useCallback, useEffect, useRef, useState } from 'react';
import type { PagedResult } from '../types/api';
import { useNotification } from '../contexts/NotificationContext';

interface UsePagedDataResult<TItem, TQuery> {
  data: PagedResult<TItem> | null;
  isLoading: boolean;
  query: TQuery;
  /** Merges a partial change into the query and returns to page 1 unless the page itself changed. */
  setQuery: (changes: Partial<TQuery>) => void;
  reload: () => void;
}

/**
 * Loads a paged list and keeps its query in state.
 *
 * Shared by every list screen so paging, sorting, filtering and error handling
 * behave identically across the application.
 */
export function usePagedData<TItem, TQuery extends { page?: number; pageSize?: number }>(
  fetcher: (query: TQuery) => Promise<PagedResult<TItem>>,
  initialQuery: TQuery,
): UsePagedDataResult<TItem, TQuery> {
  const [data, setData] = useState<PagedResult<TItem> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [query, setQueryState] = useState<TQuery>(initialQuery);
  const [reloadToken, setReloadToken] = useState(0);
  const { notifyError } = useNotification();

  // Held in a ref so a fetcher defined inline in a component does not retrigger
  // the effect on every render.
  const fetcherRef = useRef(fetcher);
  fetcherRef.current = fetcher;

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);

    fetcherRef
      .current(query)
      .then((result) => {
        // Guards against a slow earlier request overwriting a newer result.
        if (!cancelled) {
          setData(result);
        }
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          notifyError(error);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setIsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [query, reloadToken, notifyError]);

  const setQuery = useCallback((changes: Partial<TQuery>) => {
    setQueryState((previous) => {
      const isPagingChange = 'page' in changes || 'pageSize' in changes;

      return {
        ...previous,
        ...changes,

        // Changing a filter while on page 5 would otherwise show an empty page
        // and read as "no results".
        ...(isPagingChange ? {} : { page: 1 }),
      };
    });
  }, []);

  const reload = useCallback(() => setReloadToken((t) => t + 1), []);

  return { data, isLoading, query, setQuery, reload };
}
