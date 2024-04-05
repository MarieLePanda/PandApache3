# PandApache: A Web Server for Static Sites

PandApache is a lightweight web server designed specifically for hosting static websites, including HTML, CSS, and JavaScript files. It is an ideal tool for quickly and efficiently deploying presentation sites, portfolios, or landing pages, without the complexity of a dynamic backend.

## Features

- **Lightweight and Fast**: Engineered for maximum efficiency, perfect for hosting static sites.
- **Easy to Start**: Minimalist configuration for a quick setup.
- **Supports HTML, CSS, and JS Files**: Serves a wide range of static content.
- **Handles GET Requests Only**: Currently, the server processes only GET requests, ideal for serving static files without complex server-to-server interactions.
- **HTTP Configuration**: The server is configured to work with HTTP protocols, making it easy to integrate into standard web environments.

## Prerequisites

- **Docker**: PandApache is containerized, which means you will need Docker installed on your system to build and run it. Dependency management and runtime environment are handled by the Docker images used in the `Dockerfile`.

## Installation and Startup with Docker

To build and start the PandApache server using Docker, follow these steps:

1. Clone the PandApache GitHub repository:
   ```bash
   git clone [url_du_dépôt]
	```
2. Navigate to the cloned project folder:
    ```bash
   cd pandapache
 	```
3. Build the Docker image:
    ```bash
    docker build -t pandapache .
    ```
4. Run the container:
    ```bash
    docker run -d -p 5000:80 pandapache
 	```

  This command will start the server and expose the service on port 5000 of your local machine. You can access your static site by navigating to http://localhost:5000 in your browser.

## Deployment

To deploy your static site with PandApache, simply place your HTML, CSS, and JavaScript files in a www folder at the root of your project before building the Docker image. 
The Dockerfile is configured to copy this folder into the container, making your static files accessible via the web server.