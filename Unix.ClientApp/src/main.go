package main

import (
	"net/http"

	log "github.com/sirupsen/logrus"
)

func main() {
	http.HandleFunc("/", serveHome)
	http.HandleFunc("/docs", serveDocs)
	http.HandleFunc("/docs/configuration", serveConfigDocs)
	log.Info("Setting up application on port 8080.")
	if err := http.ListenAndServe(":8080", nil); err != nil {
		log.Fatal(err)
	}
}

func serveHome(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, "./static/index.html")
}

func serveDocs(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, "./static/docs.html")
}

func serveConfigDocs(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, "./static/docpages/config.html")
}
