// node_modules/@hey-api/client-fetch/dist/index.js
var A = async (t, r) => {
  let e = typeof r == "function" ? await r(t) : r;
  if (e) return t.scheme === "bearer" ? `Bearer ${e}` : t.scheme === "basic" ? `Basic ${btoa(e)}` : e;
};
var R = { bodySerializer: (t) => JSON.stringify(t, (r, e) => typeof e == "bigint" ? e.toString() : e) };
var U = (t) => {
  switch (t) {
    case "label":
      return ".";
    case "matrix":
      return ";";
    case "simple":
      return ",";
    default:
      return "&";
  }
};
var _ = (t) => {
  switch (t) {
    case "form":
      return ",";
    case "pipeDelimited":
      return "|";
    case "spaceDelimited":
      return "%20";
    default:
      return ",";
  }
};
var D = (t) => {
  switch (t) {
    case "label":
      return ".";
    case "matrix":
      return ";";
    case "simple":
      return ",";
    default:
      return "&";
  }
};
var O = ({ allowReserved: t, explode: r, name: e, style: a, value: i }) => {
  if (!r) {
    let s = (t ? i : i.map((l) => encodeURIComponent(l))).join(_(a));
    switch (a) {
      case "label":
        return `.${s}`;
      case "matrix":
        return `;${e}=${s}`;
      case "simple":
        return s;
      default:
        return `${e}=${s}`;
    }
  }
  let o = U(a), n = i.map((s) => a === "label" || a === "simple" ? t ? s : encodeURIComponent(s) : y({ allowReserved: t, name: e, value: s })).join(o);
  return a === "label" || a === "matrix" ? o + n : n;
};
var y = ({ allowReserved: t, name: r, value: e }) => {
  if (e == null) return "";
  if (typeof e == "object") throw new Error("Deeply-nested arrays/objects aren\u2019t supported. Provide your own `querySerializer()` to handle these.");
  return `${r}=${t ? e : encodeURIComponent(e)}`;
};
var q = ({ allowReserved: t, explode: r, name: e, style: a, value: i }) => {
  if (i instanceof Date) return `${e}=${i.toISOString()}`;
  if (a !== "deepObject" && !r) {
    let s = [];
    Object.entries(i).forEach(([f, u]) => {
      s = [...s, f, t ? u : encodeURIComponent(u)];
    });
    let l = s.join(",");
    switch (a) {
      case "form":
        return `${e}=${l}`;
      case "label":
        return `.${l}`;
      case "matrix":
        return `;${e}=${l}`;
      default:
        return l;
    }
  }
  let o = D(a), n = Object.entries(i).map(([s, l]) => y({ allowReserved: t, name: a === "deepObject" ? `${e}[${s}]` : s, value: l })).join(o);
  return a === "label" || a === "matrix" ? o + n : n;
};
var H = /\{[^{}]+\}/g;
var B = ({ path: t, url: r }) => {
  let e = r, a = r.match(H);
  if (a) for (let i of a) {
    let o = false, n = i.substring(1, i.length - 1), s = "simple";
    n.endsWith("*") && (o = true, n = n.substring(0, n.length - 1)), n.startsWith(".") ? (n = n.substring(1), s = "label") : n.startsWith(";") && (n = n.substring(1), s = "matrix");
    let l = t[n];
    if (l == null) continue;
    if (Array.isArray(l)) {
      e = e.replace(i, O({ explode: o, name: n, style: s, value: l }));
      continue;
    }
    if (typeof l == "object") {
      e = e.replace(i, q({ explode: o, name: n, style: s, value: l }));
      continue;
    }
    if (s === "matrix") {
      e = e.replace(i, `;${y({ name: n, value: l })}`);
      continue;
    }
    let f = encodeURIComponent(s === "label" ? `.${l}` : l);
    e = e.replace(i, f);
  }
  return e;
};
var E = ({ allowReserved: t, array: r, object: e } = {}) => (i) => {
  let o = [];
  if (i && typeof i == "object") for (let n in i) {
    let s = i[n];
    if (s != null) {
      if (Array.isArray(s)) {
        o = [...o, O({ allowReserved: t, explode: true, name: n, style: "form", value: s, ...r })];
        continue;
      }
      if (typeof s == "object") {
        o = [...o, q({ allowReserved: t, explode: true, name: n, style: "deepObject", value: s, ...e })];
        continue;
      }
      o = [...o, y({ allowReserved: t, name: n, value: s })];
    }
  }
  return o.join("&");
};
var P = (t) => {
  if (!t) return "stream";
  let r = t.split(";")[0]?.trim();
  if (r) {
    if (r.startsWith("application/json") || r.endsWith("+json")) return "json";
    if (r === "multipart/form-data") return "formData";
    if (["application/", "audio/", "image/", "video/"].some((e) => r.startsWith(e))) return "blob";
    if (r.startsWith("text/")) return "text";
  }
};
var I = async ({ security: t, ...r }) => {
  for (let e of t) {
    let a = await A(e, r.auth);
    if (!a) continue;
    let i = e.name ?? "Authorization";
    switch (e.in) {
      case "query":
        r.query || (r.query = {}), r.query[i] = a;
        break;
      case "cookie":
        r.headers.append("Cookie", `${i}=${a}`);
        break;
      case "header":
      default:
        r.headers.set(i, a);
        break;
    }
    return;
  }
};
var S = (t) => W({ baseUrl: t.baseUrl, path: t.path, query: t.query, querySerializer: typeof t.querySerializer == "function" ? t.querySerializer : E(t.querySerializer), url: t.url });
var W = ({ baseUrl: t, path: r, query: e, querySerializer: a, url: i }) => {
  let o = i.startsWith("/") ? i : `/${i}`, n = (t ?? "") + o;
  r && (n = B({ path: r, url: n }));
  let s = e ? a(e) : "";
  return s.startsWith("?") && (s = s.substring(1)), s && (n += `?${s}`), n;
};
var C = (t, r) => {
  let e = { ...t, ...r };
  return e.baseUrl?.endsWith("/") && (e.baseUrl = e.baseUrl.substring(0, e.baseUrl.length - 1)), e.headers = x(t.headers, r.headers), e;
};
var x = (...t) => {
  let r = new Headers();
  for (let e of t) {
    if (!e || typeof e != "object") continue;
    let a = e instanceof Headers ? e.entries() : Object.entries(e);
    for (let [i, o] of a) if (o === null) r.delete(i);
    else if (Array.isArray(o)) for (let n of o) r.append(i, n);
    else o !== void 0 && r.set(i, typeof o == "object" ? JSON.stringify(o) : o);
  }
  return r;
};
var h = class {
  _fns;
  constructor() {
    this._fns = [];
  }
  clear() {
    this._fns = [];
  }
  exists(r) {
    return this._fns.indexOf(r) !== -1;
  }
  eject(r) {
    let e = this._fns.indexOf(r);
    e !== -1 && (this._fns = [...this._fns.slice(0, e), ...this._fns.slice(e + 1)]);
  }
  use(r) {
    this._fns = [...this._fns, r];
  }
};
var T = () => ({ error: new h(), request: new h(), response: new h() });
var N = E({ allowReserved: false, array: { explode: true, style: "form" }, object: { explode: true, style: "deepObject" } });
var Q = { "Content-Type": "application/json" };
var w = (t = {}) => ({ ...R, headers: Q, parseAs: "auto", querySerializer: N, ...t });
var J = (t = {}) => {
  let r = C(w(), t), e = () => ({ ...r }), a = (n) => (r = C(r, n), e()), i = T(), o = async (n) => {
    let s = { ...r, ...n, fetch: n.fetch ?? r.fetch ?? globalThis.fetch, headers: x(r.headers, n.headers) };
    s.security && await I({ ...s, security: s.security }), s.body && s.bodySerializer && (s.body = s.bodySerializer(s.body)), (s.body === void 0 || s.body === "") && s.headers.delete("Content-Type");
    let l = S(s), f = { redirect: "follow", ...s }, u = new Request(l, f);
    for (let p of i.request._fns) u = await p(u, s);
    let k = s.fetch, c = await k(u);
    for (let p of i.response._fns) c = await p(c, u, s);
    let m = { request: u, response: c };
    if (c.ok) {
      if (c.status === 204 || c.headers.get("Content-Length") === "0") return { data: {}, ...m };
      let p = (s.parseAs === "auto" ? P(c.headers.get("Content-Type")) : s.parseAs) ?? "json";
      if (p === "stream") return { data: c.body, ...m };
      let b = await c[p]();
      return p === "json" && (s.responseValidator && await s.responseValidator(b), s.responseTransformer && (b = await s.responseTransformer(b))), { data: b, ...m };
    }
    let g = await c.text();
    try {
      g = JSON.parse(g);
    } catch {
    }
    let d = g;
    for (let p of i.error._fns) d = await p(g, c, u, s);
    if (d = d || {}, s.throwOnError) throw d;
    return { error: d, ...m };
  };
  return { buildUrl: S, connect: (n) => o({ ...n, method: "CONNECT" }), delete: (n) => o({ ...n, method: "DELETE" }), get: (n) => o({ ...n, method: "GET" }), getConfig: e, head: (n) => o({ ...n, method: "HEAD" }), interceptors: i, options: (n) => o({ ...n, method: "OPTIONS" }), patch: (n) => o({ ...n, method: "PATCH" }), post: (n) => o({ ...n, method: "POST" }), put: (n) => o({ ...n, method: "PUT" }), request: o, setConfig: a, trace: (n) => o({ ...n, method: "TRACE" }) };
};

// generated/client.gen.ts
var client = J(w({
  baseUrl: "https://itmd544-midterm-atdsanc8azbyeha3.canadacentral-01.azurewebsites.net"
}));

// generated/sdk.gen.ts
var taskServiceList = (options) => {
  return (options?.client ?? client).get({
    url: "/",
    ...options
  });
};
var taskServiceCreate = (options) => {
  return (options.client ?? client).post({
    url: "/",
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...options?.headers
    }
  });
};
var taskServiceDelete = (options) => {
  return (options.client ?? client).delete({
    url: "/{id}",
    ...options
  });
};
var taskServiceSummary = (options) => {
  return (options?.client ?? client).get({
    url: "/summary",
    ...options
  });
};

// app.ts
client.setConfig({ baseUrl: window.location.origin });
var taskTableBody = document.getElementById("task-table-body");
var summaryOutput = document.getElementById("summary-output");
var createForm = document.getElementById("create-form");
var statusMessage = document.getElementById("status-message");
function showStatus(msg, isError = false) {
  statusMessage.textContent = msg;
  statusMessage.style.color = isError ? "#e74c3c" : "#2ecc71";
  setTimeout(() => {
    statusMessage.textContent = "";
  }, 3e3);
}
async function loadTasks() {
  const { data, error } = await taskServiceList();
  if (error) {
    showStatus("Failed to load tasks", true);
    return;
  }
  taskTableBody.innerHTML = "";
  if (data && data.length > 0) {
    data.forEach((task) => {
      const row = document.createElement("tr");
      row.innerHTML = `
                <td>${task.id}</td>
                <td>${task.title}</td>
                <td>${task.assignee}</td>
                <td><span class="status-badge status-${task.status.toLowerCase()}">${task.status}</span></td>
                <td>${task.hours}</td>
                <td>${task.dueDate}</td>
                <td><button class="btn-delete" data-id="${task.id}">Delete</button></td>
            `;
      taskTableBody.appendChild(row);
    });
  } else {
    taskTableBody.innerHTML = '<tr><td colspan="7" style="text-align:center;">No tasks found</td></tr>';
  }
}
async function deleteTask(id) {
  const { error } = await taskServiceDelete({ path: { id } });
  if (error) {
    showStatus(`Failed to delete task: ${id}`, true);
    return;
  }
  showStatus("Task deleted successfully");
  await loadTasks();
}
async function createTask(e) {
  e.preventDefault();
  const form = e.target;
  const formData = new FormData(form);
  const body = {
    title: formData.get("title"),
    assignee: formData.get("assignee"),
    status: formData.get("status"),
    hours: parseInt(formData.get("hours"), 10),
    dueDate: formData.get("dueDate")
  };
  const { error } = await taskServiceCreate({ body });
  if (error) {
    showStatus("Failed to create task", true);
    return;
  }
  showStatus("Task created successfully");
  form.reset();
  await loadTasks();
}
async function loadSummary() {
  const { data, error } = await taskServiceSummary();
  if (error) {
    showStatus("Failed to load summary", true);
    return;
  }
  summaryOutput.textContent = JSON.stringify(data, null, 2);
}
createForm.addEventListener("submit", createTask);
document.getElementById("btn-load").addEventListener("click", loadTasks);
document.getElementById("btn-summary").addEventListener("click", loadSummary);
taskTableBody.addEventListener("click", (e) => {
  const target = e.target;
  if (target.classList.contains("btn-delete")) {
    const id = target.getAttribute("data-id");
    deleteTask(id);
  }
});
loadTasks();
