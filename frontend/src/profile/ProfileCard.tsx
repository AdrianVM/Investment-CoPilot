import { useQuery } from '@tanstack/react-query'
import { getCurrentUserOptions } from '../api/generated/@tanstack/react-query.gen'

/** Shows the signed-in user's profile from GET /api/me. */
export function ProfileCard() {
  const { data: profile, isPending, isError } = useQuery(getCurrentUserOptions())

  if (isPending) {
    return <p>Loading your profile…</p>
  }

  if (isError) {
    return <p role="alert">Your profile could not be loaded.</p>
  }

  return (
    <section aria-labelledby="profile-heading">
      <h2 id="profile-heading">Your profile</h2>
      <dl>
        <dt>Name</dt>
        <dd>{profile.displayName ?? 'Not provided'}</dd>
        <dt>Email</dt>
        <dd>{profile.email ?? 'Not provided'}</dd>
      </dl>
    </section>
  )
}
