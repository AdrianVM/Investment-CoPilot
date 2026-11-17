/** Joins class names, skipping falsy ones. */
export function classes(...names: (string | false | null | undefined)[]) {
  return names.filter(Boolean).join(' ')
}
