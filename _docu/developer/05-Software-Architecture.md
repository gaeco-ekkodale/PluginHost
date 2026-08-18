# Software Architecture

This document describes the software architecture of the PluginHost application.

## Overview

The PluginHost application consists of a backend service and a frontend client. The backend is a .NET 8 application that provides a REST API for managing the plugins. The frontend is a React application that consumes the API and displays the plugins.

## Backend Architecture

The backend is a monolithic application that is divided into several layers:

- **API Layer**: This layer is responsible for handling the incoming HTTP requests and sending the responses. It uses controllers to handle the requests and DTOs to transfer the data.
- **Service Layer**: This layer contains the business logic of the application. It uses services to implement the business logic and repositories to access the data.
- **Data Access Layer**: This layer is responsible for accessing the data from the database. It uses Entity Framework Core to access the data and repositories to abstract the data access.

## Frontend Architecture

The frontend is a single-page application (SPA) that is built with React. It uses the following components:

- **App**: The root component of the application.
- **Router**: The router component that handles the routing.
- **Pages**: The pages of the application.
- **Components**: The reusable components of the application.
- **API Client**: The API client that communicates with the backend.

## Communication

The frontend communicates with the backend via a REST API. The backend can use SignalR to send real-time updates to the frontend.
