/*global
    piranha
*/

piranha.accountsecurity = new Vue({
    el: "#accountsecurity",
    data: {
        loading: true,
        promptSetup: false,
        passkeys: [],
        registeringPasskey: false,
        totpStatus: { enrolled: false, confirmedAt: null },
        totpEnrollment: null,
        totpConfirmCode: "",
        totpBusy: false
    },
    computed: {
        // Passkeys and a confirmed authenticator app are the "strong"
        // methods - password is always available as the baseline fallback,
        // so it doesn't count here. Used to warn before removing the last
        // one of these.
        strongMethodCount: function () {
            return this.passkeys.length + (this.totpStatus.enrolled ? 1 : 0);
        }
    },
    methods: {
        load: function () {
            var self = this;
            self.loading = true;

            Promise.all([
                fetch(piranha.baseUrl + "manager/api/passkey").then(function (r) { return r.json(); }),
                fetch(piranha.baseUrl + "manager/api/totp").then(function (r) { return r.json(); })
            ]).then(function (results) {
                self.passkeys = results[0];
                self.totpStatus = results[1];
                self.loading = false;
            }).catch(function (error) {
                console.log("error:", error);
                self.loading = false;
            });
        },
        loadPasskeys: function () {
            var self = this;

            fetch(piranha.baseUrl + "manager/api/passkey")
                .then(function (response) { return response.json(); })
                .then(function (result) { self.passkeys = result; })
                .catch(function (error) { console.log("error:", error); });
        },
        registerPasskey: function () {
            var self = this;

            if (!window.PublicKeyCredential) {
                piranha.notifications.push({
                    body: "This browser doesn't support passkeys.",
                    type: "danger",
                    hide: true
                });
                return;
            }

            var deviceName = window.prompt("Name this passkey (e.g. \"Work laptop\")");
            if (!deviceName) {
                return;
            }

            self.registeringPasskey = true;

            fetch(piranha.baseUrl + "manager/api/passkey/register/options", {
                method: "post",
                headers: piranha.utils.antiForgeryHeaders()
            })
                .then(function (response) { return response.json(); })
                .then(function (challenge) {
                    var options = PublicKeyCredential.parseCreationOptionsFromJSON(JSON.parse(challenge.optionsJson));

                    return navigator.credentials.create({ publicKey: options })
                        .then(function (credential) {
                            return fetch(piranha.baseUrl + "manager/api/passkey/register/complete", {
                                method: "post",
                                headers: Object.assign({ "Content-Type": "application/json" }, piranha.utils.antiForgeryHeaders()),
                                body: JSON.stringify({
                                    token: challenge.token,
                                    deviceName: deviceName,
                                    attestationResponse: credential.toJSON()
                                })
                            });
                        });
                })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error("The passkey could not be registered.");
                    }
                    return response.json();
                })
                .then(function (result) {
                    self.passkeys = result;
                    self.registeringPasskey = false;

                    piranha.notifications.push({
                        body: "The passkey was registered.",
                        type: "success",
                        hide: true
                    });
                })
                .catch(function (error) {
                    console.log("error:", error);
                    self.registeringPasskey = false;

                    piranha.notifications.push({
                        body: "The passkey could not be registered.",
                        type: "danger",
                        hide: true
                    });
                });
        },
        renamePasskey: function (passkey) {
            var self = this;
            var deviceName = window.prompt("Rename this passkey:", passkey.deviceName);
            if (!deviceName || deviceName === passkey.deviceName) {
                return;
            }

            fetch(piranha.baseUrl + "manager/api/passkey/" + passkey.id, {
                method: "patch",
                headers: Object.assign({ "Content-Type": "application/json" }, piranha.utils.antiForgeryHeaders()),
                body: JSON.stringify({ deviceName: deviceName })
            })
                .then(function (response) { return response.json(); })
                .then(function (result) { self.passkeys = result; })
                .catch(function (error) { console.log("error:", error); });
        },
        removePasskey: function (passkey) {
            var self = this;

            var body = "Are you sure you want to remove \"" + passkey.deviceName + "\"?";
            if (self.strongMethodCount <= 1) {
                body += " This is your only passkey or authenticator app - after removing it, you'll only be able to sign in with your password.";
            }

            piranha.alert.open({
                title: piranha.resources.texts.delete,
                body: body,
                confirmCss: "btn-danger",
                confirmIcon: "fas fa-trash",
                confirmText: piranha.resources.texts.delete,
                onConfirm: function () {
                    var password = window.prompt("Enter your password to remove this passkey:");
                    if (!password) {
                        return;
                    }

                    fetch(piranha.baseUrl + "manager/api/passkey/" + passkey.id, {
                        method: "delete",
                        headers: Object.assign({ "Content-Type": "application/json" }, piranha.utils.antiForgeryHeaders()),
                        body: JSON.stringify({ password: password })
                    })
                        .then(function (response) {
                            if (!response.ok) {
                                return response.json().then(function (msg) { throw new Error(msg); });
                            }
                            self.loadPasskeys();
                        })
                        .catch(function (error) {
                            piranha.notifications.push({
                                body: error.message || "The passkey could not be removed.",
                                type: "danger",
                                hide: true
                            });
                        });
                }
            });
        },
        beginTotpEnrollment: function () {
            var self = this;
            var password = null;

            if (self.totpStatus.enrolled) {
                password = window.prompt("Enter your password to set up a new authenticator:");
                if (!password) {
                    return;
                }
            }

            self.totpBusy = true;

            fetch(piranha.baseUrl + "manager/api/totp/enroll", {
                method: "post",
                headers: Object.assign({ "Content-Type": "application/json" }, piranha.utils.antiForgeryHeaders()),
                body: JSON.stringify({ password: password })
            })
                .then(function (response) {
                    if (!response.ok) {
                        return response.json().then(function (msg) { throw new Error(msg); });
                    }
                    return response.json();
                })
                .then(function (result) {
                    self.totpEnrollment = result;
                    self.totpConfirmCode = "";
                    self.totpBusy = false;
                })
                .catch(function (error) {
                    self.totpBusy = false;
                    piranha.notifications.push({
                        body: error.message || "The authenticator could not be set up.",
                        type: "danger",
                        hide: true
                    });
                });
        },
        confirmTotpEnrollment: function () {
            var self = this;
            self.totpBusy = true;

            fetch(piranha.baseUrl + "manager/api/totp/confirm", {
                method: "post",
                headers: Object.assign({ "Content-Type": "application/json" }, piranha.utils.antiForgeryHeaders()),
                body: JSON.stringify({ token: self.totpEnrollment.token, code: self.totpConfirmCode })
            })
                .then(function (response) {
                    if (!response.ok) {
                        return response.json().then(function (msg) { throw new Error(msg); });
                    }
                    return response.json();
                })
                .then(function (result) {
                    self.totpStatus = result;
                    self.totpEnrollment = null;
                    self.totpBusy = false;

                    piranha.notifications.push({
                        body: "The authenticator app was confirmed.",
                        type: "success",
                        hide: true
                    });
                })
                .catch(function (error) {
                    self.totpBusy = false;
                    piranha.notifications.push({
                        body: error.message || "The code is incorrect or has expired.",
                        type: "danger",
                        hide: true
                    });
                });
        },
        cancelTotpEnrollment: function () {
            // Nothing has been persisted yet - the pending secret only
            // lives inside the (now discarded) enrollment token.
            this.totpEnrollment = null;
            this.totpConfirmCode = "";
        },
        revokeTotp: function () {
            var self = this;

            var body = "Are you sure you want to remove your authenticator app?";
            if (self.strongMethodCount <= 1) {
                body += " This is your only passkey or authenticator app - after removing it, you'll only be able to sign in with your password.";
            }

            piranha.alert.open({
                title: piranha.resources.texts.delete,
                body: body,
                confirmCss: "btn-danger",
                confirmIcon: "fas fa-trash",
                confirmText: piranha.resources.texts.delete,
                onConfirm: function () {
                    var password = window.prompt("Enter your password to remove your authenticator:");
                    if (!password) {
                        return;
                    }

                    fetch(piranha.baseUrl + "manager/api/totp", {
                        method: "delete",
                        headers: Object.assign({ "Content-Type": "application/json" }, piranha.utils.antiForgeryHeaders()),
                        body: JSON.stringify({ password: password })
                    })
                        .then(function (response) {
                            if (!response.ok) {
                                return response.json().then(function (msg) { throw new Error(msg); });
                            }
                            self.totpStatus = { enrolled: false, confirmedAt: null };
                        })
                        .catch(function (error) {
                            piranha.notifications.push({
                                body: error.message || "The authenticator could not be removed.",
                                type: "danger",
                                hide: true
                            });
                        });
                }
            });
        }
    }
});
