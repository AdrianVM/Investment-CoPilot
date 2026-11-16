import { UserManager } from 'oidc-client-ts'
import { createOidcSettings } from './oidcSettings'

/** The app's single sign-in manager. */
export const userManager = new UserManager(createOidcSettings(import.meta.env, window.location.origin))
