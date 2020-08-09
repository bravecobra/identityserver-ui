/// <reference path="oidc-client.js" />

function log() {
    document.getElementById('results').innerText = '';

    Array.prototype.forEach.call(arguments, function (msg) {
        if (msg instanceof Error) {
            msg = "Error: " + msg.message;
        }
        else if (typeof msg !== 'string') {
            msg = JSON.stringify(msg, null, 2);
        }
        document.getElementById('results').innerHTML += msg + '\r\n';
    });
}

function extractHostname(url, tld) {
    let hostname;

    //find & remove protocol (http, ftp, etc.) and get hostname
    if (url.indexOf("://") > -1) {
        hostname = url.split('/')[2];
    } else {
        hostname = url.split('/')[0];
    }

    //find & remove port number
    hostname = hostname.split(':')[0];

    //find & remove "?"
    hostname = hostname.split('?')[0];

    if (tld) {
        let hostnames = hostname.split('.');
        hostname = hostnames[hostnames.length - 2] + '.' + hostnames[hostnames.length - 1];
    }

    return hostname;
}

document.getElementById("login").addEventListener("click", login, false);
document.getElementById("api").addEventListener("click", api, false);
document.getElementById("logout").addEventListener("click", logout, false);

var hostname = location.hostname;
var protocol = location.protocol;

if (hostname === "localhost") {
    if (protocol === "https:") {
        var config = {
            authority: "https://localhost:5000",
            client_id: "js",
            redirect_uri: "https://localhost:5003/callback.html",
            response_type: "code",
            scope: "openid profile api1",
            post_logout_redirect_uri: "https://localhost:5003/index.html",
        };
        var apiDomain = "https://localhost:5001";
    }
    else{
        var config = {
            authority: "https://localhost:5000",
            client_id: "js",
            redirect_uri: "http://localhost:5003/callback.html",
            response_type: "code",
            scope: "openid profile api1",
            post_logout_redirect_uri: "http://localhost:5003/index.html",
        };
        var apiDomain = "http://localhost:5001";
    }

} else {
    var topleveldomain = extractHostname(hostname, true);
    var config = {
        authority: "https://sts." + topleveldomain,
        client_id: "js",
        redirect_uri: "https://jsclient."+topleveldomain+"/callback.html",
        response_type: "code",
        scope: "openid profile api1",
        post_logout_redirect_uri: "https://jsclient."+topleveldomain+"/index.html",
    };
    var apiDomain = "https://api."+topleveldomain;
}
var mgr = new Oidc.UserManager(config);

mgr.getUser().then(function (user) {
    if (user) {
        log("User logged in", user.profile);
    }
    else {
        log("User not logged in");
    }
});

function login() {
    mgr.signinRedirect();
}

function api() {
    mgr.getUser().then(function (user) {
        var url = apiDomain+"/identity";

        var xhr = new XMLHttpRequest();
        xhr.open("GET", url);
        xhr.onload = function () {
            log(xhr.status, JSON.parse(xhr.responseText));
        }
        xhr.setRequestHeader("Authorization", "Bearer " + user.access_token);
        xhr.send();
    });
}

function logout() {
    mgr.signoutRedirect();
}