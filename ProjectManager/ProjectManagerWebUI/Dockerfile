# Build stage
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration production

# Runtime stage
FROM node:20-alpine
WORKDIR /app

RUN npm install -g serve

COPY --from=build /app/dist/ProjectManagerWebUI ./dist/ProjectManagerWebUI

EXPOSE 4200

ENV NODE_ENV=production

CMD ["serve", "-s", "dist/ProjectManagerWebUI", "-l", "4200"]
