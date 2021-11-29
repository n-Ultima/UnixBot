package main

import (
	"net/http"

	log "github.com/sirupsen/logrus"
)

func main() {
	http.Handle("/", http.FileServer(http.Dir("./static")))
	http.HandleFunc("/docs", serveDocs)
	http.HandleFunc("/docs/configuration", serveConfigDocs)
	http.HandleFunc("/docs/automod", serveAutomodDocs)
	http.HandleFunc("/docs/moderation", serveModDocs)
	http.HandleFunc("/docs/infraction", serveInfractionDocs)
	http.HandleFunc("/docs/role", serveRoleDocs)
	http.HandleFunc("/docs/utility", serveUtilDocs)
	log.Info("Setting up application on port 8080.")
	if err := http.ListenAndServe(":8080", nil); err != nil {
		log.Fatal(err)
	}
}

func serveHome(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, "./static.index.html")
}

func serveDocs(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, "./static/docs.html")
}

func serveConfigDocs(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, "./static/config.html")
}

func serveAutomodDocs(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, "./static/automod.html")
}

func serveModDocs(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, "./static/moderation.html")
}

func serveInfractionDocs(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, "./static/infraction.html")
}

func serveRoleDocs(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, "./static/role.html")
}
func serveUtilDocs(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, "./static/utility.html")
}
