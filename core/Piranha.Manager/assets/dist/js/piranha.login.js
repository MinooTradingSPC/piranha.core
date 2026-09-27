/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

/*
 * Sign-in behaviour of the manager login page, shared by every login design:
 * passkey, authenticator and email-code sign-in, the remembered username and
 * form validation. Login.cshtml loads it with the site's base url in
 * data-base-url, which the sign-in endpoints are built from.
 */
(function () {
    "use strict";

    var baseUrl = (document.currentScript && document.currentScript.dataset.baseUrl) || "/";

    (function () {
        'use strict';

        // Only the pages that offer passkeys have these elements.
        if (!document.getElementById('passkey-login-link')) {
            return;
        }

        var jsonHeaders = { 'Content-Type': 'application/json' };

        function showError(message) {
            var el = document.getElementById('passkey-login-error');
            el.textContent = message;
            el.style.display = '';
        }

        function redirectAfterSignIn(verifyResult) {
            if (verifyResult.promptStrongMethodSetup) {
                window.location = baseUrl + 'manager/account/security' + '?setup=1';
                return;
            }
            window.location = verifyResult.returnUrl;
        }

        document.getElementById('passkey-login-link').addEventListener('click', function (event) {
            event.preventDefault();

            if (!window.PublicKeyCredential) {
                showError('This browser doesn\'t support passkeys.');
                return;
            }

            var email = document.getElementById('username').value;
            if (!email) {
                showError('Enter your username or email above first, then click "Sign in with a passkey" again.');
                return;
            }

            fetch(baseUrl + 'manager/auth/options', {
                method: 'post',
                headers: jsonHeaders,
                body: JSON.stringify({ email: email })
            })
                .then(function (response) { return response.json(); })
                .then(function (optionsResult) {
                    if (optionsResult.methods.indexOf('passkey') === -1) {
                        showError('No passkey is registered for this account. Please sign in with your password instead.');
                        return null;
                    }

                    return fetch(baseUrl + 'manager/auth/passkey/assertion-options', {
                        method: 'post',
                        headers: jsonHeaders,
                        body: JSON.stringify({ token: optionsResult.token })
                    })
                        .then(function (response) { return response.json(); })
                        .then(function (challenge) {
                            var options = PublicKeyCredential.parseRequestOptionsFromJSON(JSON.parse(challenge.optionsJson));

                            return navigator.credentials.get({ publicKey: options })
                                .then(function (credential) {
                                    return fetch(baseUrl + 'manager/auth/verify', {
                                        method: 'post',
                                        headers: jsonHeaders,
                                        body: JSON.stringify({
                                            token: optionsResult.token,
                                            method: 'passkey',
                                            assertionToken: challenge.token,
                                            assertionResponse: credential.toJSON()
                                        })
                                    });
                                });
                        })
                        .then(function (response) { return response.json(); })
                        .then(function (verifyResult) {
                            if (!verifyResult.succeeded) {
                                showError('The code you entered is incorrect or has expired.');
                                return;
                            }
                            redirectAfterSignIn(verifyResult);
                        });
                })
                .catch(function (error) {
                    console.log('error:', error);
                    showError('The passkey sign-in could not be completed. Please try your password instead.');
                });
        });

        function getFlowToken(email) {
            return fetch(baseUrl + 'manager/auth/options', {
                method: 'post',
                headers: jsonHeaders,
                body: JSON.stringify({ email: email })
            }).then(function (response) { return response.json(); });
        }

        document.getElementById('totp-login-link').addEventListener('click', function (event) {
            event.preventDefault();

            var email = document.getElementById('username').value;
            if (!email) {
                showError('Enter your username or email above first, then click "Sign in with an authenticator app code" again.');
                return;
            }

            getFlowToken(email).then(function (optionsResult) {
                if (optionsResult.methods.indexOf('totp') === -1) {
                    showError('No authenticator app is registered for this account. Please sign in with your password instead.');
                    return;
                }

                // Clearing the inline display hands it back to the design's style sheet.
                document.getElementById('totp-login-form').style.display = '';
                document.getElementById('totp-login-form').dataset.token = optionsResult.token;
                document.getElementById('totp-code').focus();
            }).catch(function (error) {
                console.log('error:', error);
                showError('The authenticator sign-in could not be completed. Please try your password instead.');
            });
        });

        document.getElementById('totp-login-submit').addEventListener('click', function (event) {
            event.preventDefault();

            var token = document.getElementById('totp-login-form').dataset.token;
            var code = document.getElementById('totp-code').value;

            fetch(baseUrl + 'manager/auth/verify', {
                method: 'post',
                headers: jsonHeaders,
                body: JSON.stringify({ token: token, method: 'totp', code: code })
            })
                .then(function (response) { return response.json(); })
                .then(function (verifyResult) {
                    if (!verifyResult.succeeded) {
                        showError('The code you entered is incorrect or has expired.');
                        return;
                    }
                    redirectAfterSignIn(verifyResult);
                })
                .catch(function (error) {
                    console.log('error:', error);
                    showError('The authenticator sign-in could not be completed. Please try your password instead.');
                });
        });

        document.getElementById('recovery-login-link').addEventListener('click', function (event) {
            event.preventDefault();

            var email = document.getElementById('username').value;
            if (!email) {
                showError('Enter your username or email above first, then click "Email me a sign-in code" again.');
                return;
            }

            getFlowToken(email).then(function (optionsResult) {
                return fetch(baseUrl + 'manager/auth/email-otp/request', {
                    method: 'post',
                    headers: jsonHeaders,
                    body: JSON.stringify({ token: optionsResult.token })
                }).then(function () {
                    document.getElementById('recovery-login-form').style.display = '';
                    document.getElementById('recovery-login-form').dataset.token = optionsResult.token;
                    document.getElementById('recovery-code').focus();
                });
            }).catch(function (error) {
                console.log('error:', error);
                showError('The sign-in code could not be sent. Please try your password instead.');
            });
        });

        document.getElementById('recovery-login-submit').addEventListener('click', function (event) {
            event.preventDefault();

            var token = document.getElementById('recovery-login-form').dataset.token;
            var code = document.getElementById('recovery-code').value;

            fetch(baseUrl + 'manager/auth/verify', {
                method: 'post',
                headers: jsonHeaders,
                body: JSON.stringify({ token: token, method: 'email-otp', code: code })
            })
                .then(function (response) { return response.json(); })
                .then(function (verifyResult) {
                    if (!verifyResult.succeeded) {
                        showError('The code you entered is incorrect or has expired.');
                        return;
                    }
                    redirectAfterSignIn(verifyResult);
                })
                .catch(function (error) {
                    console.log('error:', error);
                    showError('The sign-in could not be completed. Please try your password instead.');
                });
        });
    })();

    (function () {
        'use strict';

        // Remembers only the username, in this browser, when the user
        // opts in. Storage can be unavailable (private mode, blocked
        // site data), so every access is guarded.
        var storageKey = 'piranha.manager.username';
        var username = document.getElementById('username');
        var remember = document.getElementById('remember-username');
        if (!username || !remember) {
            return;
        }

        function saveUsername() {
            try {
                if (remember.checked && username.value) {
                    localStorage.setItem(storageKey, username.value);
                } else {
                    localStorage.removeItem(storageKey);
                }
            } catch (e) { }
        }

        try {
            var saved = localStorage.getItem(storageKey);
            if (saved) {
                username.value = saved;
                remember.checked = true;
                document.getElementById('password').focus();
            }
        } catch (e) { }

        remember.addEventListener('change', saveUsername);
        username.addEventListener('input', saveUsername);
    })();

    (function () {
        'use strict';
        window.addEventListener('load', function () {
            var forms = document.getElementsByClassName('needs-validation');
            var validation = Array.prototype.filter.call(forms, function (form) {
                form.addEventListener('submit', function (event) {
                    if (form.checkValidity() === false) {
                        event.preventDefault();
                        event.stopPropagation();
                    }
                    form.classList.add('was-validated');
                }, false);
            });
        }, false);
    })();
})();
