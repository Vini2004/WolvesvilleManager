import { useState } from 'react'

/** Paginação client-side simples: fatia `items` em páginas de `size`. */
export function usePaged<T>(items: T[], size = 5) {
  const [page, setPage] = useState(0)
  const pages = Math.max(1, Math.ceil(items.length / size))
  const current = Math.min(page, pages - 1)
  const slice = items.slice(current * size, current * size + size)
  return { slice, page: current, pages, setPage }
}
