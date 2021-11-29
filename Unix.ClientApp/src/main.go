package main

import (
	"net/http"

	log "github.com/sirupsen/logrus"
)

func main() {
	fs := http.FileServer(http.Dir("./static"))
	http.Handle("/unix", http.StripPrefix("/unix", fs))
	//http.Handle("/unix", http.FileServer(http.Dir("./static")))
	http.HandleFunc("/unix/docs", serveDocs)
	http.HandleFunc("/unix/docs/configuration", serveConfigDocs)
	http.HandleFunc("/unix/docs/automod", serveAutomodDocs)
	http.HandleFunc("/unix/docs/moderation", serveModDocs)
	http.HandleFunc("/unix/docs/infraction", serveInfractionDocs)
	http.HandleFunc("/unix/docs/role", serveRoleDocs)
	http.HandleFunc("/unix/docs/utility", serveUtilDocs)
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
