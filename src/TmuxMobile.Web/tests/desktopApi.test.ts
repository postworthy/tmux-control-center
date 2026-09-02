import { renameSession } from "../desktop/desktopApi.js";

function assert(condition: boolean, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

const requests: Array<{ url: string; init?: RequestInit }> = [];
globalThis.fetch = (async (input: string | URL | Request, init?: RequestInit) => {
  const url = String(input);
  requests.push({ url, init });
  if (url === "/api/auth/csrf")
    return new Response(JSON.stringify({ token: "csrf-test" }), {
      status: 200,
      headers: { "Content-Type": "application/json" }
    });
  return new Response(null, { status: 204 });
}) as typeof fetch;

await renameSession("s_alpha/beta", "renamed session");

assert(requests.length === 2, "Rename obtains CSRF once and sends one mutation.");
const mutation = requests[1];
assert(mutation.url === "/api/sessions/s_alpha%2Fbeta/rename",
  "Rename encodes the opaque session identifier in the fixed endpoint.");
assert(mutation.init?.method === "POST", "Rename uses the protected POST operation.");
assert(mutation.init?.body === JSON.stringify({ name: "renamed session" }),
  "Rename serializes only the requested name.");
assert(new Headers(mutation.init?.headers).get("X-CSRF-TOKEN") === "csrf-test",
  "Rename carries the server-issued CSRF token.");

console.log("desktop API tests passed");
