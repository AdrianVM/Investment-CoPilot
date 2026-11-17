import { useQuery } from '@tanstack/react-query'
import { getCurrentUserOptions } from '../api/generated/@tanstack/react-query.gen'
import { Notice } from '../ui/Notice'

/** Shows the signed-in user's profile from GET /api/me, as a quiet section at the end of the page. */
export function ProfileCard() {
  const { data: profile, isPending, isError } = useQuery(getCurrentUserOptions())

  return (
    <section className="profile" aria-labelledby="profile-heading">
      <h2 id="profile-heading" className="profile__title">
        Your profile
      </h2>
      {isPending ? (
        <p className="profile__status">Loading your profile…</p>
      ) : isError ? (
        <Notice tone="danger">Your profile could not be loaded.</Notice>
      ) : (
        <dl className="profile__details">
          <div>
            <dt>Name</dt>
            <dd>{profile.displayName ?? 'Not provided'}</dd>
          </div>
          <div>
            <dt>Email</dt>
            <dd>{profile.email ?? 'Not provided'}</dd>
          </div>
        </dl>
      )}
    </section>
  )
}
