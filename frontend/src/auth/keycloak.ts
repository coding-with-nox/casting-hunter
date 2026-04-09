import Keycloak from 'keycloak-js';

export const KEYCLOAK_ENABLED = false; //import.meta.env.VITE_KEYCLOAK_ENABLED === 'true';

export const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080',
  realm: import.meta.env.VITE_KEYCLOAK_REALM ?? 'myrealm',
  clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'actoring-app',
});

export async function initAuth(): Promise<void> {
  if (!KEYCLOAK_ENABLED) return;
  const authenticated = await keycloak.init({
    onLoad: 'login-required',
    checkLoginIframe: false,
  });
  if (!authenticated) keycloak.login();
}

export function getAuthToken(): string | undefined {
  return KEYCLOAK_ENABLED ? keycloak.token : undefined;
}

export async function refreshAuthToken(): Promise<void> {
  if (!KEYCLOAK_ENABLED || !keycloak.token) return;
  try {
    await keycloak.updateToken(30);
  } catch {
    keycloak.login();
    throw new Error('Session expired');
  }
}
