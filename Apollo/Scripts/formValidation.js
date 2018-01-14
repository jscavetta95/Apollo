function validateForm(callingForm) {
    if (formsFilled(callingForm.elements)) {
        if (callingForm.id === "register") {
            var emailRegex = /^[-a-z0-9~!$%^&*_=+}{\'?]+(\.[-a-z0-9~!$%^&*_=+}{\'?]+)*@([a-z0-9_][-a-z0-9_]*(\.[-a-z0-9_]+[a-z][a-z])|([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}))(:[0-9]{1,5})?$/i;
            if (!emailRegex.test(document.getElementById("reg_email").value)) {
                alert("Invalid Email");
                return false;
            }
            return true;
        }
    } else {
        alert("All forms must be filled");
        return false;
    }
}

function formsFilled(elements) {
    for (var i = 0; i < elements.length; i++) {
        if ((elements[i].type === "text" || elements[i].type === "password") && elements[i].value === "") {
            return false;
        }
    }
    return true;
}

function validatePassword(callingForm) {
    if (formsFilled(callingForm.elements)) {
        if (document.getElementById("change_password").value === document.getElementById("change_password_validate").value) {
            return true;
        } else {
            alert("Passwords do not match");
            return false;
        }
    } else {
        alert("All forms must be filled");
        return false;
    }
}