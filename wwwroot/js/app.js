const BASE_URL = window.location.origin;

async function login() {
    const emailInput = document.getElementById("loginEmail");
    const passwordInput = document.getElementById("loginPassword");

    const emailError = document.getElementById("loginEmailError");
    const passwordError = document.getElementById("loginPasswordError");
    const result = document.getElementById("loginResult");

    emailError.innerText = "";
    passwordError.innerText = "";
    result.innerText = "";
    result.className = "result-text";

    emailInput.classList.remove("input-error", "input-success");
    passwordInput.classList.remove("input-error", "input-success");

    const response = await fetch(`${BASE_URL}/api/Auth/login`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            email: emailInput.value.trim(),
            password: passwordInput.value.trim()
        })
    });

    const contentType = response.headers.get("content-type") || "";
    const text = await response.text();

    if (contentType.includes("text/html")) {
        result.innerText = "Request error: the API returned HTML instead of JSON/text.";
        result.className = "result-text general-error";
        return;
    }

    if (!response.ok) {
        if (text === "Email is required." || text === "Invalid email format." || text === "User not found. Please register first.") {
            emailError.innerText = text;
            emailInput.classList.add("input-error");
        } else if (text === "Password is required." || text === "Incorrect password.") {
            passwordError.innerText = text;
            passwordInput.classList.add("input-error");
        }

        result.innerText = text;
        result.className = "result-text general-error";
        return;
    }

    result.innerText = text;
    result.className = "result-text general-success";
    emailInput.classList.add("input-success");
    passwordInput.classList.add("input-success");
}

async function registerUser() {
    const fullNameInput = document.getElementById("regName");
    const emailInput = document.getElementById("regEmail");
    const passwordInput = document.getElementById("regPassword");

    const nameError = document.getElementById("regNameError");
    const emailError = document.getElementById("regEmailError");
    const passwordError = document.getElementById("regPasswordError");
    const result = document.getElementById("registerResult");

    nameError.innerText = "";
    emailError.innerText = "";
    passwordError.innerText = "";
    result.innerText = "";
    result.className = "result-text";

    fullNameInput.classList.remove("input-error", "input-success");
    emailInput.classList.remove("input-error", "input-success");
    passwordInput.classList.remove("input-error", "input-success");

    const response = await fetch(`${BASE_URL}/api/Auth/register`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            fullName: fullNameInput.value.trim(),
            email: emailInput.value.trim(),
            password: passwordInput.value.trim()
        })
    });

    const contentType = response.headers.get("content-type") || "";
    const text = await response.text();

    if (contentType.includes("text/html")) {
        result.innerText = "Request error: the API returned HTML instead of JSON/text.";
        result.className = "result-text general-error";
        return;
    }

    if (!response.ok) {
        if (text === "Full name is required." || text === "Full name can only contain letters and spaces.") {
            nameError.innerText = text;
            fullNameInput.classList.add("input-error");
        } else if (text === "Email is required." || text === "Invalid email format." || text === "An account with this email already exists.") {
            emailError.innerText = text;
            emailInput.classList.add("input-error");
        } else if (text === "Password is required." || text.includes("Password must be at least 8 characters")) {
            passwordError.innerText = text;
            passwordInput.classList.add("input-error");
        }

        result.innerText = text;
        result.className = "result-text general-error";
        return;
    }

    result.innerText = text;
    result.className = "result-text general-success";
    fullNameInput.classList.add("input-success");
    emailInput.classList.add("input-success");
    passwordInput.classList.add("input-success");
}

async function searchJobs() {
    const keyword = document.getElementById("searchKeyword").value.trim();
    const resultBox = document.getElementById("jobsResult");
    const resultText = document.getElementById("searchResult");

    resultBox.innerHTML = "";
    resultText.innerText = "";
    resultText.className = "result-text";

    const response = await fetch(`${BASE_URL}/api/Jobs/search?keyword=${encodeURIComponent(keyword)}`);

    if (!response.ok) {
        const text = await response.text();
        resultText.innerText = text || "Could not load jobs.";
        resultText.classList.add("general-error");
        return;
    }

    const data = await response.json();

    if (data.length === 0) {
        resultText.innerText = "No jobs found.";
        resultText.classList.add("general-error");
        return;
    }

    resultText.innerText = `${data.length} job(s) found.`;
    resultText.classList.add("general-success");

    data.forEach(job => {
        resultBox.innerHTML += `
            <div class="item-card">
                <h4>${job.title}</h4>
                <p><strong>Description:</strong> ${job.description}</p>
                <p><strong>Location:</strong> ${job.location}</p>
            </div>
        `;
    });
}

async function uploadFile() {
    const fileInput = document.getElementById("resumeFile");
    const result = document.getElementById("uploadResult");
    const formData = new FormData();

    result.innerText = "";
    result.className = "result-text";

    if (!fileInput.files[0]) {
        result.innerText = "Please select a PDF file.";
        result.classList.add("general-error");
        return;
    }

    formData.append("file", fileInput.files[0]);

    const response = await fetch(`${BASE_URL}/api/Jobs/upload`, {
        method: "POST",
        body: formData
    });

    const text = await response.text();

    if (!response.ok) {
        result.innerText = text;
        result.classList.add("general-error");
        return;
    }

    result.innerText = text;
    result.classList.add("general-success");
}

async function loadApplications() {
    const userId = document.getElementById("adminUserId").value.trim();
    const resultBox = document.getElementById("adminResult");
    const message = document.getElementById("adminMessage");

    resultBox.innerHTML = "";
    message.innerText = "";
    message.className = "result-text";

    const response = await fetch(`${BASE_URL}/api/Admin/applications?userId=${encodeURIComponent(userId)}`);

    if (!response.ok) {
        const text = await response.text();
        message.innerText = text || "Could not load applications.";
        message.classList.add("general-error");
        return;
    }

    const data = await response.json();

    if (data.length === 0) {
        message.innerText = "No applications found.";
        message.classList.add("general-error");
        return;
    }

    message.innerText = `${data.length} application(s) loaded.`;
    message.classList.add("general-success");

    data.forEach(app => {
        resultBox.innerHTML += `
            <div class="item-card">
                <h4>Application #${app.applicationId}</h4>
                <p><strong>User ID:</strong> ${app.userId}</p>
                <p><strong>Job ID:</strong> ${app.jobId}</p>
                <p><strong>Status:</strong> ${app.status}</p>
            </div>
        `;
    });
}