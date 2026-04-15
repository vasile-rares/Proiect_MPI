const { createProxyMiddleware } = require("http-proxy-middleware");

module.exports = function (app) {
  app.use(
    createProxyMiddleware({
      target: "http://localhost:5232",
      changeOrigin: true,
      pathFilter: "/api",
    })
  );
};
