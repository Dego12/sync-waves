FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm install --legacy-peer-deps

COPY . .

ARG REACT_APP_AUTH_URL
ARG REACT_APP_DOC_URL
ARG REACT_APP_RTC_URL
ARG REACT_APP_MS_CLIENT_ID
ARG REACT_APP_MS_TENANT_ID
ARG REACT_APP_MS_REDIRECT_URI

ENV REACT_APP_AUTH_URL=${REACT_APP_AUTH_URL}
ENV REACT_APP_DOC_URL=${REACT_APP_DOC_URL}
ENV REACT_APP_RTC_URL=${REACT_APP_RTC_URL}
ENV REACT_APP_MS_CLIENT_ID=${REACT_APP_MS_CLIENT_ID}
ENV REACT_APP_MS_REDIRECT_URI=${REACT_APP_MS_REDIRECT_URI}

RUN echo "AUTH_URL=$REACT_APP_AUTH_URL" && \
    echo "DOC_URL=$REACT_APP_DOC_URL" && \
    echo "RTC_URL=$REACT_APP_RTC_URL"

RUN npm run build

FROM nginx:1.27-alpine AS final
WORKDIR /usr/share/nginx/html

RUN rm -rf ./*
COPY --from=build /app/build ./

COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
