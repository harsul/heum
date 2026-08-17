/** Decodes the payload of a JWT without verifying its signature (verification happens server-side). */
export function decodeJwtPayload<T = unknown>(token: string): T | undefined {
  try {
    const payload = token.split('.')[1];
    if (!payload) return undefined;

    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map((char) => `%${char.charCodeAt(0).toString(16).padStart(2, '0')}`)
        .join(''),
    );

    return JSON.parse(json) as T;
  } catch {
    return undefined;
  }
}
