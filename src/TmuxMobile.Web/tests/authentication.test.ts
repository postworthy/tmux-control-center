import * as mobile from "../src/api.js";
import * as desktop from "../desktop/desktopApi.js";
import { authenticationRequired, setAuthenticationRequired, subscribeAuthentication } from "../src/authentication.js";

function assert(condition: boolean, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

for (const [name, api] of [["mobile", mobile], ["desktop", desktop]] as const) {
  setAuthenticationRequired(false);
  let notifications = 0;
  const unsubscribe = subscribeAuthentication(() => { notifications++; });
  let status = 204;
  let loginStatus = 204;
  let csrfCount = 0;
  const mutations: RequestInit[] = [];
  globalThis.fetch = (async (input: string | URL | Request, init?: RequestInit) => {
    const path = String(input);
    if (path === "/api/auth/login") return new Response(null, { status: loginStatus });
    if (path === "/api/auth/csrf") {
      csrfCount++;
      return new Response(JSON.stringify({ token: `token-${csrfCount}` }));
    }
    if (init?.method === "POST") {
      mutations.push(init);
      return status === 200
        ? new Response(JSON.stringify({ id: "session-1", name: "work" }))
        : new Response(null, { status });
    }
    return new Response("[]");
  }) as typeof fetch;

  await api.login("test-key");
  status = 401;
  await api.createSession("work").then(
    () => { throw new Error("Expected expired authentication"); },
    error => assert(error instanceof api.UnauthorizedError, `${name}: typed 401`));
  assert(authenticationRequired(), `${name}: create requires login`);
  assert(mutations.length === 1, `${name}: no mutation replay`);
  await api.getSessions();
  assert(authenticationRequired(), `${name}: successful inventory cannot hide prompt`);
  loginStatus = 401;
  await api.login("incorrect").catch(() => {});
  assert(authenticationRequired(), `${name}: failed login retains prompt`);
  loginStatus = 204;
  await api.login("test-key");
  assert(!authenticationRequired(), `${name}: login clears prompt`);
  status = 200;
  const created = await api.createSession("work");
  assert(created.id === "session-1", `${name}: manual retry succeeds`);
  assert(csrfCount === 2, `${name}: retry obtains fresh CSRF`);
  assert(new Headers(mutations[1].headers).get("X-CSRF-TOKEN") === "token-2",
    `${name}: retry sends renewed token`);
  status = 403;
  await api.createSession("work").catch(() => {});
  assert(!authenticationRequired(), `${name}: forbidden is not expired login`);
  assert(notifications === 2, `${name}: only auth transitions notify`);
  unsubscribe();
}
console.log("authentication recovery tests passed for desktop and mobile");
