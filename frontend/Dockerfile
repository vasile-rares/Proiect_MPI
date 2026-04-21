# Stage 1 – build
FROM node:20.19.0-alpine AS builder
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# Stage 2 – serve
FROM nginx:1.27-alpine
COPY --from=builder /app/dist /usr/share/nginx/html

# nginx:alpine processes files in /etc/nginx/templates/ with envsubst
# and writes them to /etc/nginx/conf.d/ before starting.
# Required env var: BACKEND_URL (e.g. http://backend:8080)
ENV PORT=80
ENV BACKEND_URL=http://backend:8080
COPY nginx.conf.template /etc/nginx/templates/default.conf.template

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
