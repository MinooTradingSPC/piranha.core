/*global
    piranha userlist
*/

piranha.useredit= new Vue({
    el: "#useredit",
    data: {
        loading: true,
        isNew: false,
        userModel: null,
        currentUserName: null,
        passkeys: [],
        passkeysLoading: false,
        registeringPasskey: false,
        totpStatus: { enrolled: false, confirmedAt: null },
        totpEnrollment: null,
        totpConfirmCode: "",
        totpBusy: false
    },
    computed: {
        // Passkeys are self-service: registering one has to happen in the
        // owner's own browser, so this section only ever shows for the
        // account you're signed in as, never when an admin edits someone
        // else's account.
        isEditingSelf: function () {
            return !this.isNew && this.userModel &&
                this.currentUserName === this.userModel.user.userName;
        }
    },
    methods: {
        bind: function (result) {
            this.userModel = result;
            this.isNew = result.user.id === "00000000-0000-0000-0000-000000000000";

            if (this.isEditingSelf) {
                this.loadPasskeys();
                this.loadTotpStatus();
            }
        },
        load: function (id, isNew) {
            var self = this;

            var url = isNew ? piranha.baseUrl + "manager/user/add" : piranha.baseUrl + "manager/user/edit/" + id;

            fetch(url)
                .then(function (response) { return response.json(); })
                .then(function (result) {
                    self.bind(result);
                    self.loading = false;
                })
                .catch(function (error) { console.log("error:", error); });
        },
        loadPasskeys: function () {
            var self = this;
            self.passkeysLoading = true;

            fetch(piranha.baseUrl + "manager/api/passkey")
                .then(function (response) { return response.json(); })
                .then(function (result) {
                    self.passkeys = result;
                    self.passkeysLoading = false;
                })
                .catch(function (error) {
                    console.log("error:", error);
                    self.passkeysLoading = false;
                });
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
        removePasskey: function (id) {
            var self = this;

            piranha.alert.open({
                title: piranha.resources.texts.delete,
                body: "Are you sure you want to remove this passkey?",
                confirmCss: "btn-danger",
                confirmIcon: "fas fa-trash",
                confirmText: piranha.resources.texts.delete,
                onConfirm: function () {
                    fetch(piranha.baseUrl + "manager/api/passkey/" + id, {
                        method: "delete",
                        headers: piranha.utils.antiForgeryHeaders()
                    })
                        .then(function () { self.loadPasskeys(); })
                        .catch(function (error) { console.log("error:", error); });
                }
            });
        },
        loadTotpStatus: function () {
            var self = this;

            fetch(piranha.baseUrl + "manager/api/totp")
                .then(function (response) { return response.json(); })
                .then(function (result) { self.totpStatus = result; })
                .catch(function (error) { console.log("error:", error); });
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
                    self.loadTotpStatus();
                })
                .catch(function (error) {
                    piranha.notifications.push({
                        body: error.message || "The authenticator could not be removed.",
                        type: "danger",
                        hide: true
                    });
                });
        },
        getRoleRows: function () {
            var roleRows = Array();
            for (var i = 0, j = this.userModel.roles.length; i < j; i += 3) {
                roleRows.push(this.userModel.roles.slice(i, i + 3));
            }
            return roleRows;
        },
        save: function () {
            // Validate form
            var form = document.getElementById("usereditForm");
            if (form.checkValidity() === false) {
                form.classList.add("was-validated");
                return;
            }

            var ok = false;
            var self = this;
            console.log(JSON.stringify(self.userModel));
            fetch(piranha.baseUrl + "manager/user/save", {
                method: "post",
                headers: piranha.utils.antiForgeryHeaders(),
                body: JSON.stringify(self.userModel)
            })
            .then(function (response) {
                ok = response.ok;
                return response.json();
            })
            .then(function (result) {
                if (ok) {
                    self.bind(result);
                    
                    piranha.notifications.push({
                        body: piranha.resources.texts.userIsSaved,
                        type: "success",
                        hide: true
                    });
                }
                else if (result.status) {
                    piranha.notifications.push(result.status);
                }
                else {
                    piranha.notifications.push({
                        body: "<strong>" + piranha.resources.texts.errorOccurred + "</strong>",
                        type: "danger",
                        hide: true
                    });
                }

            })
            .catch(function (error) {
                piranha.notifications.push({
                    body: error,
                    type: "danger",
                    hide: true
                });

                console.log("error:", error);
            });
        },
        remove: function (userId) {
            var self = this;

            piranha.alert.open({
                title: piranha.resources.texts.delete,
                body: piranha.resources.texts.deleteUserConfirm,
                confirmCss: "btn-danger",
                confirmIcon: "fas fa-trash",
                confirmText: piranha.resources.texts.delete,
                onConfirm: function () {
                    var ok = false;
                    fetch(piranha.baseUrl + "manager/user/delete", {
                        method: "delete",
                        headers: piranha.utils.antiForgeryHeaders(),
                        body: JSON.stringify(userId)
                    })
                    .then(function (response) { 
                        ok = response.ok;
                        return response.json();
                    })
                    .then(function (result) {
                        piranha.notifications.push(result.status);
                        if (ok) {
                            window.location = piranha.baseUrl + "manager/users/?d=1";
                        }
                    })
                    .catch(function (error) {
                        console.log("error:", error);

                        piranha.notifications.push({
                            body: error,
                            type: "danger",
                            hide: true
                        });
                    });
                }
            });
        }
    }
});
