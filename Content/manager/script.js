const titles = {
    "panel-dashboard": "Dashboard",
    "panel-users": "Manage Users",
    "panel-department": "Manage Department",
    "panel-slider": "Manage Slider",
    "panel-general-notice": "General Notices",
    "panel-event": "Events",
    "panel-video": "Video",
    "panel-gallery": "Manage Gallery",
    "panel-timetable": "Manage Time Table",
    "panel-syllabus": "Manage Syllabus",
    "panel-staff": "Teaching Staff",
    "panel-press": "Manage Press Release",
    "panel-academic": "Academic Calendar",
};

function navTo(targetId) {
    document
        .querySelectorAll(".section-panel")
        .forEach((p) => p.classList.remove("active"));
    const target = document.getElementById(targetId);
    if (target) target.classList.add("active");

    // Tables inside a previously hidden panel need their widths recalculated.
    if (target && window.jQuery && $.fn.DataTable) {
        setTimeout(() => {
            target.querySelectorAll("table.table-modern").forEach((table) => {
                if ($.fn.DataTable.isDataTable(table))
                    $(table).DataTable().columns.adjust();
            });
        }, 0);
    }

    const topTitle = document.getElementById("topTitle");
    if (topTitle) topTitle.textContent = titles[targetId] || "Dashboard";

    // sidebar active states
    document
        .querySelectorAll(".sidebar-menu > .nav-link")
        .forEach((l) => l.classList.remove("active"));
    document
        .querySelectorAll(".submenu a")
        .forEach((l) => l.classList.remove("active"));

    const directLink = document.querySelector(
        `.sidebar-menu > .nav-link[data-target="${targetId}"]`,
    );
    if (directLink) {
        directLink.classList.add("active");
    } else {
        const subLink = document.querySelector(
            `.submenu a[data-target="${targetId}"]`,
        );
        if (subLink) {
            subLink.classList.add("active");
            // open parent submenu
            const submenuUl = subLink.closest(".submenu");
            submenuUl.classList.add("show");
            const submenuToggle = document.querySelector(".submenu-toggle");
            if (submenuToggle) submenuToggle.classList.add("open");
        }
    }

    // close sidebar on mobile after nav
    if (window.innerWidth <= 991) {
        document.body.classList.remove("sidebar-open");
    }
    window.scrollTo({ top: 0, behavior: "smooth" });
}

document
    .querySelectorAll(".sidebar-menu > .nav-link[data-target]")
    .forEach((link) => {
        link.addEventListener("click", () => {
            // The href performs the actual MVC page load. This call keeps the UI
            // responsive until navigation completes and also supports same-page links.
            navTo(link.getAttribute("data-target"));
        });
    });

document.querySelectorAll(".submenu a[data-target]").forEach((link) => {
    link.addEventListener("click", (e) => {
        e.stopPropagation();
        navTo(link.getAttribute("data-target"));
    });
});

const submenuToggle = document.querySelector(".submenu-toggle");
const noticeSubmenu = document.getElementById("submenuNotice");
if (submenuToggle && noticeSubmenu) {
    submenuToggle.addEventListener("click", function () {
        this.classList.toggle("open");
        noticeSubmenu.classList.toggle("show");
    });
}

// sidebar toggle
const menuToggle = document.getElementById("menuToggle");
const backdrop = document.getElementById("backdropOverlay");
if (menuToggle) {
    menuToggle.addEventListener("click", () => {
        if (window.innerWidth <= 991) {
            document.body.classList.toggle("sidebar-open");
        } else {
            document.body.classList.toggle("sidebar-collapsed");
        }
    });
}
if (backdrop) {
    backdrop.addEventListener("click", () =>
        document.body.classList.remove("sidebar-open"),
    );
}

// Initialize the MVC page after a full navigation. Each Manager view contains
// its own section, so that section is the reliable source for the active menu.
const currentPanel = document.querySelector(".content .section-panel");
if (currentPanel) {
    navTo(currentPanel.id);
}

// ===== Dynamic dashboard =====
const dashboardPanel = document.getElementById("panel-dashboard");
if (dashboardPanel) {
    const escapeDashboard = (value) =>
        String(value == null ? "" : value).replace(
            /[&<>'"]/g,
            (c) =>
                ({
                    "&": "&amp;",
                    "<": "&lt;",
                    ">": "&gt;",
                    "'": "&#39;",
                    '"': "&quot;",
                })[c],
        );
    fetch(dashboardPanel.dataset.summaryUrl, {
        headers: { "X-Requested-With": "XMLHttpRequest" },
        credentials: "same-origin",
    })
        .then((r) => {
            if (!r.ok) throw new Error("Dashboard data could not be loaded.");
            return r.json();
        })
        .then((j) => {
            if (!j.success)
                throw new Error(j.message || "Dashboard data could not be loaded.");
            document.getElementById("dashboardDepartmentCount").textContent =
                j.counts.departments;
            document.getElementById("dashboardContentCount").textContent =
                j.counts.content;
            document.getElementById("dashboardStaffCount").textContent =
                j.counts.staff;
            document.getElementById("dashboardGalleryCount").textContent =
                j.counts.gallery;
            const body = document.getElementById("dashboardRecentNotices");
            body.innerHTML = j.recent.length
                ? j.recent
                    .map(
                        (x) =>
                            `<tr><td>${escapeDashboard(x.Title)}</td><td>${escapeDashboard(x.Category)}</td><td>${escapeDashboard(x.Date)}</td><td><span class="badge-status ${x.IsActive ? "badge-active" : "badge-inactive"}">${x.IsActive ? "Published" : "Inactive"}</span></td></tr>`,
                    )
                    .join("")
                : '<tr><td colspan="4" class="text-center py-4">No notices found.</td></tr>';
        })
        .catch((e) => {
            document.getElementById("dashboardRecentNotices").innerHTML =
                '<tr><td colspan="4" class="text-center text-danger py-4">Unable to load dashboard data.</td></tr>';
            showManagerAlert("error", e.message);
        });
}

// ===== Current manager profile =====
const managerProfile = document.getElementById("managerProfile");
if (managerProfile) {
    const photo = document.getElementById("managerProfileImage"),
        picker = document.getElementById("managerProfilePhoto");
    const tokenField = document.querySelector(
        '#managerLogoutForm input[name="__RequestVerificationToken"]',
    );
    const modalElement = document.getElementById("modalChangePassword"),
        savePassword = document.getElementById("saveManagerPassword");
    const loadProfile = async () => {
        try {
            const response = await fetch(managerProfile.dataset.currentUrl, {
                credentials: "same-origin",
            });
            if (!response.ok) throw new Error("Profile could not be loaded.");
            const json = await response.json();
            if (!json.success) throw new Error(json.message);
            const p = json.data;
            photo.src = p.ImageUrl;
            document.getElementById("managerProfileName").textContent = p.DisplayName;
            document.getElementById("managerProfileRole").textContent = p.Username;
            document.getElementById("managerMenuDisplayName").textContent =
                p.DisplayName;
            document.getElementById("managerMenuUsername").textContent =
                "@" + p.Username;
        } catch (error) {
            console.error("Manager profile load failed:", error);
        }
    };
    document
        .getElementById("managerProfileCamera")
        .addEventListener("click", (e) => {
            e.preventDefault();
            e.stopPropagation();
            picker.click();
        });
    picker.addEventListener("change", async () => {
        const file = picker.files[0];
        if (!file) return;
        if (
            !["image/jpeg", "image/png", "image/webp"].includes(file.type) ||
            file.size > 3 * 1024 * 1024
        ) {
            picker.value = "";
            return showManagerAlert(
                "error",
                "Select a JPG, PNG or WEBP image up to 3 MB.",
            );
        }
        const form = new FormData();
        form.append("ProfilePhoto", file);
        form.append("__RequestVerificationToken", tokenField.value);
        try {
            const response = await fetch(managerProfile.dataset.uploadUrl, {
                method: "POST",
                body: form,
                credentials: "same-origin",
            });
            if (!response.ok) throw new Error("Profile photo upload failed.");
            const json = await response.json();
            if (!json.success) throw new Error(json.message);
            photo.src =
                json.imageUrl +
                (json.imageUrl.includes("?") ? "&" : "?") +
                "v=" +
                Date.now();
            showManagerAlert("success", json.message);
        } catch (error) {
            showManagerAlert("error", error.message);
        } finally {
            picker.value = "";
        }
    });
    modalElement.addEventListener("hidden.bs.modal", () => {
        modalElement.querySelectorAll("input").forEach((x) => (x.value = ""));
        clearManagerValidation(modalElement);
    });
    savePassword.addEventListener("click", async () => {
        clearManagerValidation(modalElement);
        savePassword.disabled = true;
        const form = new URLSearchParams({
            CurrentPassword: document.getElementById("currentManagerPassword").value,
            NewPassword: document.getElementById("newManagerPassword").value,
            ConfirmPassword: document.getElementById("confirmManagerPassword").value,
            __RequestVerificationToken: tokenField.value,
        });
        try {
            const response = await fetch(managerProfile.dataset.passwordUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                },
                body: form.toString(),
                credentials: "same-origin",
            });
            if (!response.ok) throw new Error("Password change request failed.");
            const json = await response.json();
            if (!json.success) {
                showManagerValidation(modalElement, json.errors || {});
                return showManagerAlert("error", json.message);
            }
            bootstrap.Modal.getOrCreateInstance(modalElement).hide();
            await showManagerAlert("success", json.message);
            document.getElementById("managerLogoutForm").submit();
        } catch (error) {
            showManagerAlert("error", error.message);
        } finally {
            savePassword.disabled = false;
        }
    });
    loadProfile();
}

// ===== Manage Department CRUD =====
const departmentPanel = document.getElementById("panel-department");
if (departmentPanel) {
    const tableBody = document.getElementById("departmentTableBody");
    const modalElement = document.getElementById("modalDept");
    const departmentModal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const departmentId = document.getElementById("departmentId");
    const departmentName = document.getElementById("departmentName");
    const departmentStatus = document.getElementById("departmentStatus");
    const modalTitle = document.getElementById("departmentModalTitle");
    const saveButton = document.getElementById("saveDepartmentBtn");
    const antiForgeryToken = departmentPanel.querySelector(
        'input[name="__RequestVerificationToken"]',
    ).value;
    let departments = [];

    const escapeHtml = (value) =>
        String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");

    async function postDepartment(url, values) {
        const body = new URLSearchParams(values);
        body.append("__RequestVerificationToken", antiForgeryToken);
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
            },
            body: body.toString(),
        });
        if (!response.ok) throw new Error("Request failed. Please try again.");
        return response.json();
    }

    function renderDepartments() {
        const departmentTable = tableBody.closest("table");
        if (
            window.jQuery &&
            $.fn.DataTable &&
            $.fn.DataTable.isDataTable(departmentTable)
        ) {
            $(departmentTable).DataTable().destroy();
        }

        if (!departments.length) {
            tableBody.innerHTML = "";
            window.initManagerDataTable(departmentTable);
            return;
        }

        tableBody.innerHTML = departments
            .map(
                (department, index) => `
            <tr>
                <td>${String(index + 1).padStart(2, "0")}</td>
                <td>${escapeHtml(department.DepartmentName)}</td>
                <td>
                    <button type="button" class="badge-status status-toggle ${department.IsActive ? "badge-active" : "badge-inactive"}"
                            data-action="toggle" data-id="${department.DepartmentId}"
                            title="Click to ${department.IsActive ? "deactivate" : "activate"}">
                        ${department.IsActive ? "Active" : "Inactive"}
                    </button>
                </td>
                <td>
                    <button type="button" class="action-btn edit" data-action="edit" data-id="${department.DepartmentId}" title="Edit"><i class="bi bi-pencil"></i></button>
                    <button type="button" class="action-btn del" data-action="delete" data-id="${department.DepartmentId}" title="Delete"><i class="bi bi-trash"></i></button>
                </td>
            </tr>`,
            )
            .join("");
        window.initManagerDataTable(departmentTable);
    }

    async function loadDepartments() {
        try {
            const response = await fetch(departmentPanel.dataset.listUrl, {
                cache: "no-store",
            });
            if (!response.ok) throw new Error("Unable to load departments.");
            const result = await response.json();
            if (!result.success)
                throw new Error(result.message || "Unable to load departments.");
            departments = result.data || [];
            renderDepartments();
        } catch (error) {
            tableBody.innerHTML =
                '<tr><td colspan="4" class="text-center text-danger py-4">Unable to load departments.</td></tr>';
            showManagerAlert("error", error.message);
        }
    }

    function openDepartmentModal(department) {
        clearManagerValidation(modalElement);
        const isEdit = Boolean(department);
        departmentId.value = isEdit ? department.DepartmentId : 0;
        departmentName.value = isEdit ? department.DepartmentName : "";
        departmentStatus.value = isEdit ? String(department.IsActive) : "true";
        modalTitle.textContent = isEdit ? "Edit Department" : "Add New Department";
        saveButton.textContent = isEdit ? "Update Department" : "Save Department";
        departmentModal.show();
        modalElement.addEventListener(
            "shown.bs.modal",
            () => departmentName.focus(),
            { once: true },
        );
    }

    document
        .getElementById("addDepartmentBtn")
        .addEventListener("click", () => openDepartmentModal(null));

    saveButton.addEventListener("click", async () => {
        const name = departmentName.value.trim();
        if (!name) {
            showManagerValidation(modalElement, {
                DepartmentName: "Department name is required.",
            });
            departmentName.focus();
            return;
        }

        saveButton.disabled = true;
        try {
            const result = await postDepartment(departmentPanel.dataset.saveUrl, {
                DepartmentId: departmentId.value,
                DepartmentName: name,
                IsActive: departmentStatus.value,
            });
            if (!result.success) {
                showManagerValidation(modalElement, result.errors || {});
                showManagerAlert("error", result.message);
                return;
            }
            departmentModal.hide();
            showManagerAlert("success", result.message);
            await loadDepartments();
        } catch (error) {
            showManagerAlert("error", error.message);
        } finally {
            saveButton.disabled = false;
        }
    });

    tableBody.addEventListener("click", async (event) => {
        const button = event.target.closest("[data-action]");
        if (!button) return;
        const id = Number(button.dataset.id);
        const department = departments.find((item) => item.DepartmentId === id);
        if (!department) return;

        if (button.dataset.action === "edit") {
            openDepartmentModal(department);
            return;
        }

        if (button.dataset.action === "toggle") {
            button.disabled = true;
            try {
                const result = await postDepartment(departmentPanel.dataset.toggleUrl, {
                    id: id,
                });
                showManagerAlert(result.success ? "success" : "error", result.message);
                if (result.success) await loadDepartments();
            } catch (error) {
                showManagerAlert("error", error.message);
            } finally {
                button.disabled = false;
            }
            return;
        }

        if (button.dataset.action === "delete") {
            const confirmation = await Swal.fire({
                title: "Delete department?",
                text: department.DepartmentName,
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#e0453f",
                confirmButtonText: "Yes, delete it",
            });
            if (!confirmation.isConfirmed) return;

            try {
                const result = await postDepartment(departmentPanel.dataset.deleteUrl, {
                    id: id,
                });
                showManagerAlert(result.success ? "success" : "error", result.message);
                if (result.success) await loadDepartments();
            } catch (error) {
                showManagerAlert("error", error.message);
            }
        }
    });

    loadDepartments();
}

// ===== Manage Slider CRUD =====
const sliderPanel = document.getElementById("panel-slider");
if (sliderPanel) {
    const sliderTableBody = document.getElementById("sliderTableBody");
    const sliderModalElement = document.getElementById("modalSlider");
    const sliderModal = bootstrap.Modal.getOrCreateInstance(sliderModalElement);
    const sliderId = document.getElementById("sliderId");
    const sliderTitle = document.getElementById("sliderTitle");
    const sliderDescription = document.getElementById("sliderDescription");
    const sliderStatus = document.getElementById("sliderStatus");
    const sliderImage = document.getElementById("sliderImage");
    const sliderPreview = document.getElementById("sliderImagePreview");
    const sliderUploadPrompt = document.getElementById("sliderUploadPrompt");
    const sliderDropZone = document.getElementById("sliderDropZone");
    const sliderModalTitle = document.getElementById("sliderModalTitle");
    const saveSliderBtn = document.getElementById("saveSliderBtn");
    const sliderToken = sliderPanel.querySelector(
        'input[name="__RequestVerificationToken"]',
    ).value;
    let sliders = [];
    let selectedSliderFile = null;

    const sliderEscapeHtml = (value) =>
        String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");

    async function sliderPost(url, values) {
        const body = new URLSearchParams(values);
        body.append("__RequestVerificationToken", sliderToken);
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
            },
            body: body.toString(),
        });
        if (!response.ok) throw new Error("Request failed. Please try again.");
        return response.json();
    }

    function renderSliders() {
        const table = sliderTableBody.closest("table");
        if (window.jQuery && $.fn.DataTable && $.fn.DataTable.isDataTable(table))
            $(table).DataTable().destroy();
        if (!sliders.length) {
            sliderTableBody.innerHTML = "";
            window.initManagerDataTable(table);
            return;
        }
        sliderTableBody.innerHTML = sliders
            .map(
                (slider, index) => `
            <tr>
                <td>${String(index + 1).padStart(2, "0")}</td>
                <td><img class="thumb-md" src="${sliderEscapeHtml(slider.ImageUrl)}" alt="${sliderEscapeHtml(slider.Title)}"></td>
                <td>${sliderEscapeHtml(slider.Title)}</td>
                <td>${sliderEscapeHtml(slider.SortDescription)}</td>
                <td><button type="button" class="badge-status status-toggle ${slider.IsActive ? "badge-active" : "badge-inactive"}" data-slider-action="toggle" data-id="${slider.SliderId}">${slider.IsActive ? "Active" : "Inactive"}</button></td>
                <td><button type="button" class="action-btn edit" data-slider-action="edit" data-id="${slider.SliderId}" title="Edit"><i class="bi bi-pencil"></i></button><button type="button" class="action-btn del" data-slider-action="delete" data-id="${slider.SliderId}" title="Delete"><i class="bi bi-trash"></i></button></td>
            </tr>`,
            )
            .join("");
        window.initManagerDataTable(table);
    }

    async function loadSliders() {
        try {
            const response = await fetch(sliderPanel.dataset.listUrl, {
                cache: "no-store",
            });
            if (!response.ok) throw new Error("Unable to load sliders.");
            const result = await response.json();
            if (!result.success)
                throw new Error(result.message || "Unable to load sliders.");
            sliders = result.data || [];
            renderSliders();
        } catch (error) {
            sliderTableBody.innerHTML =
                '<tr><td colspan="6" class="text-center text-danger py-4">Unable to load sliders.</td></tr>';
            showManagerAlert("error", error.message);
        }
    }

    function setSliderPreview(url) {
        if (url) {
            sliderPreview.src = url;
            sliderPreview.classList.remove("d-none");
            sliderUploadPrompt.classList.add("d-none");
        } else {
            sliderPreview.removeAttribute("src");
            sliderPreview.classList.add("d-none");
            sliderUploadPrompt.classList.remove("d-none");
        }
    }

    function selectSliderFile(file) {
        clearManagerValidation(sliderModalElement);
        if (!file) return;
        const validTypes = ["image/jpeg", "image/png", "image/webp"];
        if (!validTypes.includes(file.type)) {
            showManagerValidation(sliderModalElement, {
                ImageFile: "Only JPG, PNG or WEBP images are allowed.",
            });
            return;
        }
        if (file.size > 3 * 1024 * 1024) {
            showManagerValidation(sliderModalElement, {
                ImageFile: "Image size cannot exceed 3 MB.",
            });
            return;
        }
        selectedSliderFile = file;
        setSliderPreview(URL.createObjectURL(file));
    }

    function openSliderModal(slider) {
        clearManagerValidation(sliderModalElement);
        selectedSliderFile = null;
        sliderImage.value = "";
        sliderId.value = slider ? slider.SliderId : 0;
        sliderTitle.value = slider ? slider.Title : "";
        sliderDescription.value = slider ? slider.SortDescription : "";
        sliderStatus.value = slider ? String(slider.IsActive) : "true";
        sliderModalTitle.textContent = slider ? "Edit Slider" : "Add New Slider";
        saveSliderBtn.textContent = slider ? "Update Slider" : "Save Slider";
        setSliderPreview(slider ? slider.ImageUrl : null);
        sliderModal.show();
    }

    document
        .getElementById("addSliderBtn")
        .addEventListener("click", () => openSliderModal(null));
    sliderDropZone.addEventListener("click", () => sliderImage.click());
    sliderDropZone.addEventListener("keydown", (event) => {
        if (event.key === "Enter" || event.key === " ") sliderImage.click();
    });
    sliderImage.addEventListener("change", () =>
        selectSliderFile(sliderImage.files[0]),
    );
    ["dragenter", "dragover"].forEach((name) =>
        sliderDropZone.addEventListener(name, (event) => {
            event.preventDefault();
            sliderDropZone.classList.add("drag-over");
        }),
    );
    ["dragleave", "drop"].forEach((name) =>
        sliderDropZone.addEventListener(name, (event) => {
            event.preventDefault();
            sliderDropZone.classList.remove("drag-over");
        }),
    );
    sliderDropZone.addEventListener("drop", (event) =>
        selectSliderFile(event.dataTransfer.files[0]),
    );

    saveSliderBtn.addEventListener("click", async () => {
        const errors = {};
        if (!sliderTitle.value.trim()) errors.Title = "Title is required.";
        if (!sliderDescription.value.trim())
            errors.SortDescription = "Sort description is required.";
        if (Number(sliderId.value) === 0 && !selectedSliderFile)
            errors.ImageFile = "Slider image is required.";
        if (Object.keys(errors).length) {
            showManagerValidation(sliderModalElement, errors);
            return;
        }

        const formData = new FormData();
        formData.append("__RequestVerificationToken", sliderToken);
        formData.append("SliderId", sliderId.value);
        formData.append("Title", sliderTitle.value.trim());
        formData.append("SortDescription", sliderDescription.value.trim());
        formData.append("IsActive", sliderStatus.value);
        if (selectedSliderFile) formData.append("ImageFile", selectedSliderFile);
        saveSliderBtn.disabled = true;
        try {
            const response = await fetch(sliderPanel.dataset.saveUrl, {
                method: "POST",
                body: formData,
            });
            if (!response.ok) throw new Error("Unable to save slider.");
            const result = await response.json();
            if (!result.success) {
                showManagerValidation(sliderModalElement, result.errors || {});
                showManagerAlert("error", result.message);
                return;
            }
            sliderModal.hide();
            showManagerAlert("success", result.message);
            await loadSliders();
        } catch (error) {
            showManagerAlert("error", error.message);
        } finally {
            saveSliderBtn.disabled = false;
        }
    });

    sliderTableBody.addEventListener("click", async (event) => {
        const button = event.target.closest("[data-slider-action]");
        if (!button) return;
        const id = Number(button.dataset.id);
        const slider = sliders.find((item) => item.SliderId === id);
        if (!slider) return;
        if (button.dataset.sliderAction === "edit") return openSliderModal(slider);
        if (button.dataset.sliderAction === "toggle") {
            const result = await sliderPost(sliderPanel.dataset.toggleUrl, { id });
            showManagerAlert(result.success ? "success" : "error", result.message);
            if (result.success) await loadSliders();
            return;
        }
        const confirmation = await Swal.fire({
            title: "Delete slider?",
            text: slider.Title,
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#e0453f",
            confirmButtonText: "Yes, delete it",
        });
        if (!confirmation.isConfirmed) return;
        try {
            const result = await sliderPost(sliderPanel.dataset.deleteUrl, { id });
            showManagerAlert(result.success ? "success" : "error", result.message);
            if (result.success) await loadSliders();
        } catch (error) {
            showManagerAlert("error", error.message);
        }
    });

    loadSliders();
}

// ===== General Notice, Event and Video CRUD =====
const contentModule = document.getElementById("noticeEventVideoModule");
if (contentModule) {
    const contentToken = contentModule.querySelector(
        'input[name="__RequestVerificationToken"]',
    ).value;
    const contentEscape = (value) =>
        String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    const contentPost = async (url, values) => {
        const body = new URLSearchParams(values);
        body.append("__RequestVerificationToken", contentToken);
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
            },
            body: body.toString(),
        });
        if (!response.ok) throw new Error("Request failed. Please try again.");
        return response.json();
    };
    const resetDataTable = (tbody) => {
        const table = tbody.closest("table");
        if ($.fn.DataTable.isDataTable(table)) $(table).DataTable().destroy();
        return table;
    };

    // General Notice
    const noticeBody = document.getElementById("noticeTableBody"),
        noticeModalEl = document.getElementById("modalNotice"),
        noticeModal = bootstrap.Modal.getOrCreateInstance(noticeModalEl);
    const noticeId = document.getElementById("noticeId"),
        noticeType = document.getElementById("noticeType"),
        noticeTitle = document.getElementById("noticeTitle"),
        noticeFile = document.getElementById("noticeFile"),
        noticeStatus = document.getElementById("noticeStatus"),
        noticeModalTitle = document.getElementById("noticeModalTitle"),
        addNoticeBtn = document.getElementById("addNoticeBtn"),
        saveNoticeBtn = document.getElementById("saveNoticeBtn");
    const noticeDropZone = document.getElementById("noticeDropZone"),
        noticeFilePrompt = document.getElementById("noticeFilePrompt");
    let notices = [];
    async function loadNotices() {
        try {
            const r = await fetch(contentModule.dataset.noticeList, {
                cache: "no-store",
            }),
                j = await r.json();
            notices = j.data || [];
            const t = resetDataTable(noticeBody);
            noticeBody.innerHTML = notices
                .map(
                    (x, i) =>
                        `<tr><td>${i + 1}</td><td>${contentEscape(x.NoticeType)}</td><td>${contentEscape(x.Title)}</td><td><a href="${contentEscape(x.FileUrl)}" target="_blank" class="btn btn-sm btn-outline-primary rounded-0"><i class="bi bi-file-earmark-arrow-down"></i> View</a></td><td><button class="badge-status status-toggle ${x.IsActive ? "badge-active" : "badge-inactive"}" data-notice-action="toggle" data-id="${x.NoticeId}">${x.IsActive ? "Active" : "Inactive"}</button></td><td>${contentEscape(x.CreatedBy)}</td><td>${contentEscape(x.UpdatedBy || "-")}</td><td><button class="action-btn edit" data-notice-action="edit" data-id="${x.NoticeId}"><i class="bi bi-pencil"></i></button><button class="action-btn del" data-notice-action="delete" data-id="${x.NoticeId}"><i class="bi bi-trash"></i></button></td></tr>`,
                )
                .join("");
            initManagerDataTable(t);
        } catch (e) {
            showManagerAlert("error", "Unable to load notices.");
        }
    }
    function openNotice(x) {
        clearManagerValidation(noticeModalEl);
        noticeId.value = x ? x.NoticeId : 0;
        noticeType.value = x ? x.NoticeType : "";
        noticeTitle.value = x ? x.Title : "";
        noticeStatus.value = x ? String(x.IsActive) : "true";
        noticeFile.value = "";
        noticeFilePrompt.innerHTML =
            "Click or drag PDF/DOCX file<br><small>Maximum 3 MB; optional while editing</small>";
        noticeModalTitle.textContent = x
            ? "Edit General Notice"
            : "Add General Notice";
        noticeModal.show();
    }
    noticeDropZone.onclick = () => noticeFile.click();
    noticeDropZone.onkeydown = (e) => {
        if (e.key === "Enter" || e.key === " ") noticeFile.click();
    };
    noticeFile.onchange = () => {
        if (noticeFile.files[0])
            noticeFilePrompt.textContent = noticeFile.files[0].name;
    };
    ["dragenter", "dragover"].forEach((n) =>
        noticeDropZone.addEventListener(n, (e) => {
            e.preventDefault();
            noticeDropZone.classList.add("drag-over");
        }),
    );
    ["dragleave", "drop"].forEach((n) =>
        noticeDropZone.addEventListener(n, (e) => {
            e.preventDefault();
            noticeDropZone.classList.remove("drag-over");
        }),
    );
    noticeDropZone.addEventListener("drop", (e) => {
        if (e.dataTransfer.files[0]) {
            const dt = new DataTransfer();
            dt.items.add(e.dataTransfer.files[0]);
            noticeFile.files = dt.files;
            noticeFilePrompt.textContent = e.dataTransfer.files[0].name;
        }
    });
    addNoticeBtn.onclick = () => openNotice(null);
    saveNoticeBtn.onclick = async () => {
        const e = {};
        if (!noticeType.value) e.NoticeType = "Notice type is required.";
        if (!noticeTitle.value.trim()) e.NoticeTitle = "Title is required.";
        if (+noticeId.value === 0 && !noticeFile.files[0])
            e.NoticeFile = "PDF or DOCX file is required.";
        if (Object.keys(e).length) return showManagerValidation(noticeModalEl, e);
        const f = new FormData();
        f.append("__RequestVerificationToken", contentToken);
        f.append("NoticeId", noticeId.value);
        f.append("NoticeType", noticeType.value);
        f.append("Title", noticeTitle.value.trim());
        f.append("IsActive", noticeStatus.value);
        if (noticeFile.files[0]) f.append("NoticeFile", noticeFile.files[0]);
        saveNoticeBtn.disabled = true;
        try {
            const r = await fetch(contentModule.dataset.noticeSave, {
                method: "POST",
                body: f,
            }),
                j = await r.json();
            if (!j.success) {
                showManagerValidation(noticeModalEl, j.errors || {});
                return showManagerAlert("error", j.message);
            }
            noticeModal.hide();
            showManagerAlert("success", j.message);
            await loadNotices();
        } catch (x) {
            showManagerAlert("error", x.message);
        } finally {
            saveNoticeBtn.disabled = false;
        }
    };
    noticeBody.onclick = async (ev) => {
        const b = ev.target.closest("[data-notice-action]");
        if (!b) return;
        const x = notices.find((n) => n.NoticeId === +b.dataset.id);
        if (b.dataset.noticeAction === "edit") return openNotice(x);
        if (b.dataset.noticeAction === "delete") {
            const c = await Swal.fire({
                title: "Delete notice?",
                icon: "warning",
                showCancelButton: true,
            });
            if (!c.isConfirmed) return;
        }
        const url =
            b.dataset.noticeAction === "toggle"
                ? contentModule.dataset.noticeToggle
                : contentModule.dataset.noticeDelete;
        const j = await contentPost(url, { id: b.dataset.id });
        showManagerAlert(j.success ? "success" : "error", j.message);
        if (j.success) await loadNotices();
    };

    // Event
    const eventBody = document.getElementById("eventTableBody"),
        eventModalEl = document.getElementById("modalEvent"),
        eventModal = bootstrap.Modal.getOrCreateInstance(eventModalEl);
    let events = [];
    const eventId = document.getElementById("eventId"),
        eventName = document.getElementById("eventName"),
        eventVenue = document.getElementById("eventVenue"),
        eventDate = document.getElementById("eventDate"),
        eventTime = document.getElementById("eventTime"),
        eventStatus = document.getElementById("eventStatus"),
        eventModalTitle = document.getElementById("eventModalTitle"),
        addEventBtn = document.getElementById("addEventBtn"),
        saveEventBtn = document.getElementById("saveEventBtn");
    const eventBannerImage = document.getElementById("eventBannerImage"),
        eventBannerDropZone = document.getElementById("eventBannerDropZone"),
        eventBannerPreview = document.getElementById("eventBannerPreview"),
        eventBannerPrompt = document.getElementById("eventBannerPrompt");
    let selectedEventBanner = null;
    async function loadEvents() {
        try {
            const r = await fetch(contentModule.dataset.eventList, {
                cache: "no-store",
            }),
                j = await r.json();
            events = j.data || [];
            const t = resetDataTable(eventBody);
            eventBody.innerHTML = events
                .map(
                    (x, i) =>
                        `<tr><td>${i + 1}</td><td>${x.BannerImageUrl ? `<img class="thumb-md" src="${contentEscape(x.BannerImageUrl)}" alt="Banner">` : "-"}</td><td>${contentEscape(x.EventName)}</td><td>${contentEscape(x.Venue)}</td><td>${x.EventDate}</td><td>${x.EventTime}</td><td><button class="badge-status status-toggle ${x.IsActive ? "badge-active" : "badge-inactive"}" data-event-action="toggle" data-id="${x.EventId}">${x.IsActive ? "Active" : "Inactive"}</button></td><td>${contentEscape(x.CreatedBy)}</td><td>${contentEscape(x.UpdatedBy || "-")}</td><td><button class="action-btn edit" data-event-action="edit" data-id="${x.EventId}"><i class="bi bi-pencil"></i></button><button class="action-btn del" data-event-action="delete" data-id="${x.EventId}"><i class="bi bi-trash"></i></button></td></tr>`,
                )
                .join("");
            initManagerDataTable(t);
        } catch (e) {
            showManagerAlert("error", "Unable to load events.");
        }
    }
    function showEventBanner(url) {
        if (url) {
            eventBannerPreview.src = url;
            eventBannerPreview.classList.remove("d-none");
            eventBannerPrompt.classList.add("d-none");
        } else {
            eventBannerPreview.classList.add("d-none");
            eventBannerPreview.removeAttribute("src");
            eventBannerPrompt.classList.remove("d-none");
        }
    }
    function chooseEventBanner(file) {
        if (!file) return;
        clearManagerValidation(eventModalEl);
        if (!["image/jpeg", "image/png", "image/webp"].includes(file.type))
            return showManagerValidation(eventModalEl, {
                BannerImage: "Only JPG, PNG or WEBP images are allowed.",
            });
        if (file.size > 3 * 1024 * 1024)
            return showManagerValidation(eventModalEl, {
                BannerImage: "Image size cannot exceed 3 MB.",
            });
        selectedEventBanner = file;
        showEventBanner(URL.createObjectURL(file));
    }
    function openEvent(x) {
        clearManagerValidation(eventModalEl);
        selectedEventBanner = null;
        eventBannerImage.value = "";
        eventId.value = x ? x.EventId : 0;
        eventName.value = x ? x.EventName : "";
        eventVenue.value = x ? x.Venue : "";
        eventDate.value = x ? x.EventDate : "";
        eventTime.value = x ? x.EventTime : "";
        eventStatus.value = x ? String(x.IsActive) : "true";
        showEventBanner(x ? x.BannerImageUrl : null);
        eventModalTitle.textContent = x ? "Edit Event" : "Add New Event";
        eventModal.show();
    }
    eventBannerDropZone.onclick = () => eventBannerImage.click();
    eventBannerDropZone.onkeydown = (e) => {
        if (e.key === "Enter" || e.key === " ") eventBannerImage.click();
    };
    eventBannerImage.onchange = () =>
        chooseEventBanner(eventBannerImage.files[0]);
    ["dragenter", "dragover"].forEach((n) =>
        eventBannerDropZone.addEventListener(n, (e) => {
            e.preventDefault();
            eventBannerDropZone.classList.add("drag-over");
        }),
    );
    ["dragleave", "drop"].forEach((n) =>
        eventBannerDropZone.addEventListener(n, (e) => {
            e.preventDefault();
            eventBannerDropZone.classList.remove("drag-over");
        }),
    );
    eventBannerDropZone.addEventListener("drop", (e) =>
        chooseEventBanner(e.dataTransfer.files[0]),
    );
    addEventBtn.onclick = () => openEvent(null);
    saveEventBtn.onclick = async () => {
        const e = {};
        if (!eventName.value.trim()) e.EventName = "Event name is required.";
        if (!eventVenue.value.trim()) e.Venue = "Venue is required.";
        if (!eventDate.value) e.EventDate = "Date is required.";
        if (!eventTime.value) e.EventTime = "Time is required.";
        if (+eventId.value === 0 && !selectedEventBanner)
            e.BannerImage = "Event banner image is required.";
        if (Object.keys(e).length) return showManagerValidation(eventModalEl, e);
        const f = new FormData();
        f.append("__RequestVerificationToken", contentToken);
        f.append("EventId", eventId.value);
        f.append("EventName", eventName.value.trim());
        f.append("Venue", eventVenue.value.trim());
        f.append("EventDate", eventDate.value);
        f.append("EventTime", eventTime.value);
        f.append("IsActive", eventStatus.value);
        if (selectedEventBanner) f.append("BannerImage", selectedEventBanner);
        saveEventBtn.disabled = true;
        try {
            const r = await fetch(contentModule.dataset.eventSave, {
                method: "POST",
                body: f,
            }),
                j = await r.json();
            if (!j.success) {
                showManagerValidation(eventModalEl, j.errors || {});
                return showManagerAlert("error", j.message);
            }
            eventModal.hide();
            showManagerAlert("success", j.message);
            await loadEvents();
        } catch (x) {
            showManagerAlert("error", x.message);
        } finally {
            saveEventBtn.disabled = false;
        }
    };
    eventBody.onclick = async (ev) => {
        const b = ev.target.closest("[data-event-action]");
        if (!b) return;
        const x = events.find((n) => n.EventId === +b.dataset.id);
        if (b.dataset.eventAction === "edit") return openEvent(x);
        if (b.dataset.eventAction === "delete") {
            const c = await Swal.fire({
                title: "Delete event?",
                icon: "warning",
                showCancelButton: true,
            });
            if (!c.isConfirmed) return;
        }
        const j = await contentPost(
            b.dataset.eventAction === "toggle"
                ? contentModule.dataset.eventToggle
                : contentModule.dataset.eventDelete,
            { id: b.dataset.id },
        );
        showManagerAlert(j.success ? "success" : "error", j.message);
        if (j.success) await loadEvents();
    };

    // Video
    const videoBody = document.getElementById("videoTableBody"),
        videoModalEl = document.getElementById("modalVideo"),
        videoModal = bootstrap.Modal.getOrCreateInstance(videoModalEl);
    let videos = [];
    const videoId = document.getElementById("videoId"),
        videoTitle = document.getElementById("videoTitle"),
        videoSource = document.getElementById("videoSource"),
        videoStatus = document.getElementById("videoStatus"),
        videoModalTitle = document.getElementById("videoModalTitle"),
        addVideoBtn = document.getElementById("addVideoBtn"),
        saveVideoBtn = document.getElementById("saveVideoBtn");
    async function loadVideos() {
        try {
            const r = await fetch(contentModule.dataset.videoList, {
                cache: "no-store",
            }),
                j = await r.json();
            videos = j.data || [];
            const t = resetDataTable(videoBody);
            videoBody.innerHTML = videos
                .map(
                    (x, i) =>
                        `<tr><td>${i + 1}</td><td>${contentEscape(x.Title)}</td><td><a href="${contentEscape(x.SourceUrl)}" target="_blank" rel="noopener" class="btn btn-sm btn-outline-primary rounded-0">Open Source</a></td><td><button class="badge-status status-toggle ${x.IsActive ? "badge-active" : "badge-inactive"}" data-video-action="toggle" data-id="${x.VideoId}">${x.IsActive ? "Active" : "Inactive"}</button></td><td>${contentEscape(x.CreatedBy)}</td><td>${contentEscape(x.UpdatedBy || "-")}</td><td><button class="action-btn edit" data-video-action="edit" data-id="${x.VideoId}"><i class="bi bi-pencil"></i></button><button class="action-btn del" data-video-action="delete" data-id="${x.VideoId}"><i class="bi bi-trash"></i></button></td></tr>`,
                )
                .join("");
            initManagerDataTable(t);
        } catch (e) {
            showManagerAlert("error", "Unable to load videos.");
        }
    }
    function openVideo(x) {
        clearManagerValidation(videoModalEl);
        videoId.value = x ? x.VideoId : 0;
        videoTitle.value = x ? x.Title : "";
        videoSource.value = x ? x.SourceUrl : "";
        videoStatus.value = x ? String(x.IsActive) : "true";
        videoModalTitle.textContent = x ? "Edit Video" : "Add New Video";
        videoModal.show();
    }
    addVideoBtn.onclick = () => openVideo(null);
    saveVideoBtn.onclick = async () => {
        const e = {};
        if (!videoTitle.value.trim()) e.VideoTitle = "Title is required.";
        if (!videoSource.value.trim()) e.SourceUrl = "Source URL is required.";
        if (Object.keys(e).length) return showManagerValidation(videoModalEl, e);
        saveVideoBtn.disabled = true;
        try {
            const j = await contentPost(contentModule.dataset.videoSave, {
                VideoId: videoId.value,
                Title: videoTitle.value.trim(),
                SourceUrl: videoSource.value.trim(),
                IsActive: videoStatus.value,
            });
            if (!j.success) {
                showManagerValidation(videoModalEl, j.errors || {});
                return showManagerAlert("error", j.message);
            }
            videoModal.hide();
            showManagerAlert("success", j.message);
            await loadVideos();
        } finally {
            saveVideoBtn.disabled = false;
        }
    };
    videoBody.onclick = async (ev) => {
        const b = ev.target.closest("[data-video-action]");
        if (!b) return;
        const x = videos.find((n) => n.VideoId === +b.dataset.id);
        if (b.dataset.videoAction === "edit") return openVideo(x);
        if (b.dataset.videoAction === "delete") {
            const c = await Swal.fire({
                title: "Delete video?",
                icon: "warning",
                showCancelButton: true,
            });
            if (!c.isConfirmed) return;
        }
        const j = await contentPost(
            b.dataset.videoAction === "toggle"
                ? contentModule.dataset.videoToggle
                : contentModule.dataset.videoDelete,
            { id: b.dataset.id },
        );
        showManagerAlert(j.success ? "success" : "error", j.message);
        if (j.success) await loadVideos();
    };
    loadNotices();
    loadEvents();
    loadVideos();
}

// ===== Manage Gallery and Category Master =====
const galleryPanel = document.getElementById("panel-gallery");
if (galleryPanel) {
    const token = galleryPanel.querySelector(
        'input[name="__RequestVerificationToken"]',
    ).value,
        body = document.getElementById("galleryTableBody");
    const modalEl = document.getElementById("modalGallery"),
        modal = bootstrap.Modal.getOrCreateInstance(modalEl),
        categoryModalEl = document.getElementById("modalGalleryCategory"),
        categoryModal = bootstrap.Modal.getOrCreateInstance(categoryModalEl);
    const id = document.getElementById("galleryId"),
        category = document.getElementById("galleryCategory"),
        categorySelected = document.getElementById("galleryCategorySelected"),
        title = document.getElementById("galleryTitle"),
        status = document.getElementById("galleryStatus"),
        imageInput = document.getElementById("galleryImage"),
        preview = document.getElementById("galleryImagePreview"),
        prompt = document.getElementById("galleryUploadPrompt"),
        dropZone = document.getElementById("galleryDropZone");
    const categoryName = document.getElementById("galleryCategoryName"),
        saveBtn = document.getElementById("saveGalleryBtn"),
        saveCategoryBtn = document.getElementById("saveGalleryCategoryBtn");
    let rows = [],
        selectedFile = null;
    const esc = (v) =>
        String(v || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    async function post(url, values) {
        const b = new URLSearchParams(values);
        b.append("__RequestVerificationToken", token);
        const r = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
            },
            body: b.toString(),
        });
        if (!r.ok) throw new Error("Request failed.");
        return r.json();
    }
    const categoryLabel = (value) => {
        const text = String(value || "").trim();
        return text.length > 32 ? text.slice(0, 29) + "..." : text;
    };
    function showSelectedCategory() {
        const option = category.options[category.selectedIndex];
        const full =
            option && option.dataset.fullName ? option.dataset.fullName : "";
        categorySelected.textContent =
            full && full !== option.textContent ? full : "";
    }
    async function loadCategories(selectId) {
        const r = await fetch(galleryPanel.dataset.categoryListUrl, {
            cache: "no-store",
        }),
            j = await r.json();
        category.innerHTML =
            '<option value="">Select category</option>' +
            j.data
                .map(
                    (x) =>
                        `<option value="${x.CategoryId}" data-full-name="${esc(x.CategoryName)}" title="${esc(x.CategoryName)}">${esc(categoryLabel(x.CategoryName))}</option>`,
                )
                .join("") +
            '<option value="other">Other</option>';
        if (selectId) category.value = String(selectId);
        showSelectedCategory();
    }
    function showPreview(url) {
        if (url) {
            preview.src = url;
            preview.classList.remove("d-none");
            prompt.classList.add("d-none");
        } else {
            preview.removeAttribute("src");
            preview.classList.add("d-none");
            prompt.classList.remove("d-none");
        }
    }
    function selectFile(file) {
        if (!file) return;
        clearManagerValidation(modalEl);
        if (!["image/jpeg", "image/png", "image/webp"].includes(file.type))
            return showManagerValidation(modalEl, {
                GalleryImage: "Only JPG, PNG or WEBP images are allowed.",
            });
        if (file.size > 3 * 1024 * 1024)
            return showManagerValidation(modalEl, {
                GalleryImage: "Image size cannot exceed 3 MB.",
            });
        selectedFile = file;
        showPreview(URL.createObjectURL(file));
    }
    async function loadRows() {
        try {
            const r = await fetch(galleryPanel.dataset.listUrl, {
                cache: "no-store",
            }),
                j = await r.json();
            rows = j.data || [];
            const table = body.closest("table");
            if ($.fn.DataTable.isDataTable(table)) $(table).DataTable().destroy();
            body.innerHTML = rows
                .map(
                    (x, i) =>
                        `<tr><td>${i + 1}</td><td><img class="thumb-md" src="${esc(x.ImageUrl)}" alt="Gallery"></td><td>${esc(x.CategoryName)}</td><td>${esc(x.Title || "-")}</td><td><button class="badge-status status-toggle ${x.IsActive ? "badge-active" : "badge-inactive"}" data-gallery-action="toggle" data-id="${x.GalleryId}">${x.IsActive ? "Active" : "Inactive"}</button></td><td>${esc(x.CreatedBy)}</td><td>${esc(x.UpdatedBy || "-")}</td><td><button class="action-btn edit" data-gallery-action="edit" data-id="${x.GalleryId}"><i class="bi bi-pencil"></i></button><button class="action-btn del" data-gallery-action="delete" data-id="${x.GalleryId}"><i class="bi bi-trash"></i></button></td></tr>`,
                )
                .join("");
            initManagerDataTable(table);
        } catch (e) {
            showManagerAlert("error", "Unable to load gallery.");
        }
    }
    async function openGallery(x) {
        clearManagerValidation(modalEl);
        selectedFile = null;
        imageInput.value = "";
        id.value = x ? x.GalleryId : 0;
        title.value = x ? x.Title || "" : "";
        status.value = x ? String(x.IsActive) : "true";
        document.getElementById("galleryModalTitle").textContent = x
            ? "Edit Gallery"
            : "Add Gallery Image";
        showPreview(x ? x.ImageUrl : null);
        await loadCategories(x ? x.CategoryId : null);
        modal.show();
    }
    document.getElementById("addGalleryBtn").onclick = () => openGallery(null);
    dropZone.onclick = () => imageInput.click();
    dropZone.onkeydown = (e) => {
        if (e.key === "Enter" || e.key === " ") imageInput.click();
    };
    imageInput.onchange = () => selectFile(imageInput.files[0]);
    ["dragenter", "dragover"].forEach((n) =>
        dropZone.addEventListener(n, (e) => {
            e.preventDefault();
            dropZone.classList.add("drag-over");
        }),
    );
    ["dragleave", "drop"].forEach((n) =>
        dropZone.addEventListener(n, (e) => {
            e.preventDefault();
            dropZone.classList.remove("drag-over");
        }),
    );
    dropZone.addEventListener("drop", (e) => selectFile(e.dataTransfer.files[0]));
    category.onchange = () => {
        if (category.value === "other") {
            category.value = "";
            categorySelected.textContent = "";
            clearManagerValidation(categoryModalEl);
            categoryName.value = "";
            categoryModal.show();
            return;
        }
        showSelectedCategory();
    };
    function closeCategory() {
        categoryModal.hide();
        category.value = "";
    }
    document.getElementById("closeGalleryCategoryBtn").onclick = closeCategory;
    document.getElementById("cancelGalleryCategoryBtn").onclick = closeCategory;
    categoryModalEl.addEventListener("hidden.bs.modal", () => {
        if (modalEl.classList.contains("show"))
            document.body.classList.add("modal-open");
    });
    saveCategoryBtn.onclick = async () => {
        if (!categoryName.value.trim())
            return showManagerValidation(categoryModalEl, {
                CategoryName: "Category name is required.",
            });
        saveCategoryBtn.disabled = true;
        try {
            const j = await post(galleryPanel.dataset.categorySaveUrl, {
                CategoryName: categoryName.value.trim(),
            });
            if (!j.success) {
                showManagerValidation(categoryModalEl, j.errors || {});
                return showManagerAlert("error", j.message);
            }
            categoryModal.hide();
            await loadCategories(j.data.CategoryId);
            showManagerAlert("success", j.message);
        } catch (e) {
            showManagerAlert("error", e.message);
        } finally {
            saveCategoryBtn.disabled = false;
        }
    };
    saveBtn.onclick = async () => {
        const e = {};
        if (!category.value) e.GalleryCategory = "Category is required.";
        if (+id.value === 0 && !selectedFile)
            e.GalleryImage = "Gallery image is required.";
        if (title.value.length > 200)
            e.GalleryTitle = "Maximum 200 characters allowed.";
        if (Object.keys(e).length) return showManagerValidation(modalEl, e);
        const f = new FormData();
        f.append("__RequestVerificationToken", token);
        f.append("GalleryId", id.value);
        f.append("CategoryId", category.value);
        f.append("Title", title.value.trim());
        f.append("IsActive", status.value);
        if (selectedFile) f.append("GalleryImage", selectedFile);
        saveBtn.disabled = true;
        try {
            const r = await fetch(galleryPanel.dataset.saveUrl, {
                method: "POST",
                body: f,
            }),
                j = await r.json();
            if (!j.success) {
                showManagerValidation(modalEl, j.errors || {});
                return showManagerAlert("error", j.message);
            }
            modal.hide();
            showManagerAlert("success", j.message);
            await loadRows();
        } catch (e) {
            showManagerAlert("error", e.message);
        } finally {
            saveBtn.disabled = false;
        }
    };
    body.onclick = async (e) => {
        const b = e.target.closest("[data-gallery-action]");
        if (!b) return;
        const x = rows.find((r) => r.GalleryId === +b.dataset.id);
        if (b.dataset.galleryAction === "edit") return openGallery(x);
        if (b.dataset.galleryAction === "delete") {
            const c = await Swal.fire({
                title: "Delete gallery image?",
                icon: "warning",
                showCancelButton: true,
            });
            if (!c.isConfirmed) return;
        }
        const j = await post(
            b.dataset.galleryAction === "toggle"
                ? galleryPanel.dataset.toggleUrl
                : galleryPanel.dataset.deleteUrl,
            { id: b.dataset.id },
        );
        showManagerAlert(j.success ? "success" : "error", j.message);
        if (j.success) await loadRows();
    };
    loadCategories();
    loadRows();
}

// ===== Manage Time Table =====
const timeTablePanel = document.getElementById("panel-timetable");
if (timeTablePanel) {
    const token = timeTablePanel.querySelector(
        'input[name="__RequestVerificationToken"]',
    ).value,
        body = document.getElementById("timeTableBody"),
        modalEl = document.getElementById("modalTimetable"),
        modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    const id = document.getElementById("timeTableId"),
        session = document.getElementById("timeTableSession"),
        semester = document.getElementById("timeTableSemester"),
        semesterMenu = document.getElementById("timeTableSemesterDropdown"),
        branch = document.getElementById("timeTableBranch"),
        file = document.getElementById("timeTableFile"),
        filePrompt = document.getElementById("timeTableFilePrompt"),
        drop = document.getElementById("timeTableDropZone"),
        save = document.getElementById("saveTimeTableBtn");
    let rows = [];
    const semesterBoxes = () => [
        ...semesterMenu.querySelectorAll('input[type="checkbox"]'),
    ];
    const pickedSemesters = () =>
        semesterBoxes()
            .filter((c) => c.checked)
            .map((c) => c.value);
    function semesterLabel(values) {
        if (!values.length) return "Select semester";
        if (values.length === 1) return values[0];
        return (
            values
                .map((v) => v.replace(/(?:st|nd|rd|th)\s+Semester$/i, ""))
                .join("/") + " Semester"
        );
    }
    function syncSemesterToggle() {
        const values = pickedSemesters();
        semester.textContent = semesterLabel(values);
        semester.classList.toggle("text-muted", values.length === 0);
        if (values.length) semester.classList.remove("is-invalid");
    }
    function setSemesters(text) {
        const picked = String(text || "")
            .replace(/\s*Semester\s*$/i, "")
            .split(/[\/,]/)
            .map((t) => t.trim().replace(/(?:st|nd|rd|th)$/i, "").toUpperCase())
            .filter(Boolean);
        semesterBoxes().forEach((c) => {
            c.checked = picked.indexOf(c.dataset.roman) >= 0;
        });
        syncSemesterToggle();
    }
    semesterMenu.addEventListener("change", syncSemesterToggle);
    const esc = (v) =>
        String(v || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    async function post(url, values) {
        const b = new URLSearchParams(values);
        b.append("__RequestVerificationToken", token);
        const r = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
            },
            body: b.toString(),
        });
        if (!r.ok) throw new Error("Request failed.");
        return r.json();
    }
    async function loadRows() {
        try {
            const r = await fetch(timeTablePanel.dataset.listUrl, {
                cache: "no-store",
            }),
                j = await r.json();
            rows = j.data || [];
            const t = body.closest("table");
            if ($.fn.DataTable.isDataTable(t)) $(t).DataTable().destroy();
            body.innerHTML = rows
                .map(
                    (x, i) =>
                        `<tr><td>${i + 1}</td><td>${esc(x.SessionName)}</td><td>${esc(x.SemesterType)}</td><td>${esc(x.BranchNames)}</td><td><a class="btn btn-sm btn-outline-primary rounded-0" href="${esc(x.FileUrl)}" target="_blank"><i class="bi bi-file-earmark-arrow-down"></i> View</a></td><td><button class="badge-status status-toggle ${x.IsActive ? "badge-active" : "badge-inactive"}" data-tt-action="toggle" data-id="${x.TimeTableId}">${x.IsActive ? "Active" : "Inactive"}</button></td><td>${esc(x.CreatedBy)}</td><td>${esc(x.UpdatedBy || "-")}</td><td><button class="action-btn edit" data-tt-action="edit" data-id="${x.TimeTableId}"><i class="bi bi-pencil"></i></button><button class="action-btn del" data-tt-action="delete" data-id="${x.TimeTableId}"><i class="bi bi-trash"></i></button></td></tr>`,
                )
                .join("");
            initManagerDataTable(t);
        } catch (e) {
            showManagerAlert("error", "Unable to load time tables.");
        }
    }
    async function open(x) {
        clearManagerValidation(modalEl);
        id.value = x ? x.TimeTableId : 0;
        session.value = x ? x.SessionName : "";
        setSemesters(x ? x.SemesterType : "");
        branch.value = x ? x.BranchNames : "";
        file.value = "";
        filePrompt.innerHTML =
            "Click or drag PDF/Word file<br><small>Maximum 3 MB; optional while editing</small>";
        document.getElementById("timeTableModalTitle").textContent = x
            ? "Edit Time Table"
            : "Add Time Table";
        modal.show();
    }
    document.getElementById("addTimeTableBtn").onclick = () => open(null);
    drop.onclick = () => file.click();
    drop.onkeydown = (e) => {
        if (e.key === "Enter" || e.key === " ") file.click();
    };
    file.onchange = () => {
        if (file.files[0]) filePrompt.textContent = file.files[0].name;
    };
    ["dragenter", "dragover"].forEach((n) =>
        drop.addEventListener(n, (e) => {
            e.preventDefault();
            drop.classList.add("drag-over");
        }),
    );
    ["dragleave", "drop"].forEach((n) =>
        drop.addEventListener(n, (e) => {
            e.preventDefault();
            drop.classList.remove("drag-over");
        }),
    );
    drop.addEventListener("drop", (e) => {
        const f = e.dataTransfer.files[0];
        if (f) {
            const dt = new DataTransfer();
            dt.items.add(f);
            file.files = dt.files;
            filePrompt.textContent = f.name;
        }
    });
    save.onclick = async () => {
        const e = {};
        if (!session.value.trim())
            e.TimeTableSession = "Academic Session is required.";
        const semesters = pickedSemesters();
        if (!semesters.length)
            e.TimeTableSemester = "Select at least one semester.";
        if (!branch.value.trim()) e.TimeTableBranch = "Branch is required.";
        if (+id.value === 0 && !file.files[0])
            e.TimeTableFile = "PDF or Word file is required.";
        else if (file.files[0] && file.files[0].size > 3 * 1024 * 1024)
            e.TimeTableFile = "File size cannot exceed 3 MB.";
        if (Object.keys(e).length) return showManagerValidation(modalEl, e);
        const f = new FormData();
        f.append("__RequestVerificationToken", token);
        f.append("TimeTableId", id.value);
        f.append("SessionName", session.value.trim());
        f.append("SemesterType", semesters.join(", "));
        f.append("BranchNames", branch.value.trim());
        if (file.files[0]) f.append("TimeTableFile", file.files[0]);
        save.disabled = true;
        try {
            const r = await fetch(timeTablePanel.dataset.saveUrl, {
                method: "POST",
                body: f,
            }),
                j = await r.json();
            if (!j.success) {
                showManagerValidation(modalEl, j.errors || {});
                return showManagerAlert("error", j.message);
            }
            modal.hide();
            showManagerAlert("success", j.message);
            await loadRows();
        } catch (e) {
            showManagerAlert("error", e.message);
        } finally {
            save.disabled = false;
        }
    };
    body.onclick = async (e) => {
        const b = e.target.closest("[data-tt-action]");
        if (!b) return;
        const x = rows.find((r) => r.TimeTableId === +b.dataset.id);
        if (b.dataset.ttAction === "edit") return open(x);
        if (b.dataset.ttAction === "delete") {
            const c = await Swal.fire({
                title: "Delete time table?",
                icon: "warning",
                showCancelButton: true,
            });
            if (!c.isConfirmed) return;
        }
        const j = await post(
            b.dataset.ttAction === "toggle"
                ? timeTablePanel.dataset.toggleUrl
                : timeTablePanel.dataset.deleteUrl,
            { id: b.dataset.id },
        );
        showManagerAlert(j.success ? "success" : "error", j.message);
        if (j.success) await loadRows();
    };
    loadRows();
}

// ===== Manage Syllabus =====
const syllabusPanel = document.getElementById("panel-syllabus");
if (syllabusPanel) {
    const token = syllabusPanel.querySelector(
        'input[name="__RequestVerificationToken"]',
    ).value,
        body = document.getElementById("syllabusTableBody"),
        modalEl = document.getElementById("modalSyllabus"),
        modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    const id = document.getElementById("syllabusId"),
        course = document.getElementById("syllabusCourse"),
        year = document.getElementById("syllabusYear"),
        branch = document.getElementById("syllabusBranch"),
        file = document.getElementById("syllabusFile"),
        prompt = document.getElementById("syllabusFilePrompt"),
        drop = document.getElementById("syllabusDropZone"),
        save = document.getElementById("saveSyllabusBtn");
    let rows = [];
    const esc = (v) =>
        String(v || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    async function post(url, values) {
        const b = new URLSearchParams(values);
        b.append("__RequestVerificationToken", token);
        const r = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
            },
            body: b.toString(),
        });
        if (!r.ok) throw new Error("Request failed.");
        return r.json();
    }
    async function load() {
        try {
            const r = await fetch(syllabusPanel.dataset.listUrl, {
                cache: "no-store",
            }),
                j = await r.json();
            rows = j.data || [];
            const t = body.closest("table");
            if ($.fn.DataTable.isDataTable(t)) $(t).DataTable().destroy();
            body.innerHTML = rows
                .map(
                    (x, i) =>
                        `<tr><td>${i + 1}</td><td>${esc(x.CourseName)}</td><td>${esc(x.BranchNames)}</td><td>${esc(x.StudyYear)}</td><td><a class="btn btn-sm btn-outline-primary rounded-0" href="${esc(x.FileUrl)}" target="_blank">View</a></td><td><button class="badge-status status-toggle ${x.IsActive ? "badge-active" : "badge-inactive"}" data-syllabus-action="toggle" data-id="${x.SyllabusId}">${x.IsActive ? "Active" : "Inactive"}</button></td><td>${esc(x.CreatedBy)}</td><td>${esc(x.UpdatedBy || "-")}</td><td><button class="action-btn edit" data-syllabus-action="edit" data-id="${x.SyllabusId}"><i class="bi bi-pencil"></i></button><button class="action-btn del" data-syllabus-action="delete" data-id="${x.SyllabusId}"><i class="bi bi-trash"></i></button></td></tr>`,
                )
                .join("");
            initManagerDataTable(t);
        } catch (e) {
            showManagerAlert("error", "Unable to load syllabus.");
        }
    }
    async function open(x) {
        clearManagerValidation(modalEl);
        id.value = x ? x.SyllabusId : 0;
        course.value = x ? x.CourseName : "";
        branch.value = x ? x.BranchNames : "";
        year.value = x ? x.StudyYear : "";
        file.value = "";
        prompt.innerHTML =
            "Click or drag PDF/Word file<br><small>Maximum 3 MB; optional while editing</small>";
        document.getElementById("syllabusModalTitle").textContent = x
            ? "Edit Syllabus"
            : "Add Syllabus";
        modal.show();
    }
    document.getElementById("addSyllabusBtn").onclick = () => open(null);
    drop.onclick = () => file.click();
    drop.onkeydown = (e) => {
        if (e.key === "Enter" || e.key === " ") file.click();
    };
    file.onchange = () => {
        if (file.files[0]) prompt.textContent = file.files[0].name;
    };
    ["dragenter", "dragover"].forEach((n) =>
        drop.addEventListener(n, (e) => {
            e.preventDefault();
            drop.classList.add("drag-over");
        }),
    );
    ["dragleave", "drop"].forEach((n) =>
        drop.addEventListener(n, (e) => {
            e.preventDefault();
            drop.classList.remove("drag-over");
        }),
    );
    drop.addEventListener("drop", (e) => {
        const f = e.dataTransfer.files[0];
        if (f) {
            const dt = new DataTransfer();
            dt.items.add(f);
            file.files = dt.files;
            prompt.textContent = f.name;
        }
    });
    save.onclick = async () => {
        const e = {};
        if (!course.value.trim()) e.SyllabusCourse = "Programme is required.";
        if (!branch.value.trim()) e.SyllabusBranch = "Branch is required.";
        if (!year.value) e.SyllabusYear = "Year is required.";
        if (+id.value === 0 && !file.files[0])
            e.SyllabusFile = "PDF or Word file is required.";
        if (Object.keys(e).length) return showManagerValidation(modalEl, e);
        const f = new FormData();
        f.append("__RequestVerificationToken", token);
        f.append("SyllabusId", id.value);
        f.append("CourseName", course.value.trim());
        f.append("BranchNames", branch.value.trim());
        f.append("StudyYear", year.value);
        if (file.files[0]) f.append("SyllabusFile", file.files[0]);
        save.disabled = true;
        try {
            const r = await fetch(syllabusPanel.dataset.saveUrl, {
                method: "POST",
                body: f,
            }),
                j = await r.json();
            if (!j.success) {
                showManagerValidation(modalEl, j.errors || {});
                return showManagerAlert("error", j.message);
            }
            modal.hide();
            showManagerAlert("success", j.message);
            await load();
        } catch (e) {
            showManagerAlert("error", e.message);
        } finally {
            save.disabled = false;
        }
    };
    body.onclick = async (e) => {
        const b = e.target.closest("[data-syllabus-action]");
        if (!b) return;
        const x = rows.find((r) => r.SyllabusId === +b.dataset.id);
        if (b.dataset.syllabusAction === "edit") return open(x);
        if (b.dataset.syllabusAction === "delete") {
            const c = await Swal.fire({
                title: "Delete syllabus?",
                icon: "warning",
                showCancelButton: true,
            });
            if (!c.isConfirmed) return;
        }
        const j = await post(
            b.dataset.syllabusAction === "toggle"
                ? syllabusPanel.dataset.toggleUrl
                : syllabusPanel.dataset.deleteUrl,
            { id: b.dataset.id },
        );
        showManagerAlert(j.success ? "success" : "error", j.message);
        if (j.success) await load();
    };
    load();
}

// ===== Teaching Staff with CKEditor =====
const staffPanel = document.getElementById("panel-staff");
if (staffPanel) {
    const token = staffPanel.querySelector(
        'input[name="__RequestVerificationToken"]',
    ).value,
        body = document.getElementById("staffTableBody"),
        modalEl = document.getElementById("modalStaff"),
        modal = bootstrap.Modal.getOrCreateInstance(modalEl, { focus: false }),
        descriptionModalEl = document.getElementById("modalStaffDescription"),
        descriptionModal = bootstrap.Modal.getOrCreateInstance(descriptionModalEl);
    const id = document.getElementById("staffId"),
        name = document.getElementById("staffName"),
        email = document.getElementById("staffEmail"),
        alternateEmail = document.getElementById("staffAlternateEmail"),
        mobileNumber = document.getElementById("staffMobileNumber"),
        landlineNumber = document.getElementById("staffLandlineNumber"),
        designation = document.getElementById("staffDesignation"),
        department = document.getElementById("staffDepartment"),
        qualification = document.getElementById("staffQualification"),
        displayOrder = document.getElementById("staffDisplayOrder"),
        status = document.getElementById("staffStatus"),
        photo = document.getElementById("staffPhoto"),
        preview = document.getElementById("staffPhotoPreview"),
        prompt = document.getElementById("staffPhotoPrompt"),
        drop = document.getElementById("staffPhotoDropZone"),
        save = document.getElementById("saveStaffBtn");
    let rows = [],
        selectedPhoto = null,
        staffEditor = null,
        pendingStaffDescription = "";
    const esc = (v) =>
        String(v || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    async function post(url, values) {
        const b = new URLSearchParams(values);
        b.append("__RequestVerificationToken", token);
        const r = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
            },
            body: b.toString(),
        });
        if (!r.ok) throw new Error("Request failed.");
        return r.json();
    }
    function registerStaffLineHeightPlugin() {
        if (window.CKEDITOR.plugins.registered.lineheight) return;
        window.CKEDITOR.plugins.add("lineheight", {
            requires: "richcombo",
            init: function (editor) {
                editor.ui.addRichCombo("LineHeight", {
                    label: "Line Height",
                    title: "Line Height",
                    toolbar: "styles,30",
                    panel: {
                        css: [window.CKEDITOR.skin.getPath("editor")].concat(
                            editor.config.contentsCss || [],
                        ),
                        multiSelect: false,
                    },
                    init: function () {
                        this.startGroup("Line Height");
                        const values = (
                            editor.config.lineHeight_values ||
                            "Normal/normal;1.0/1;1.15/1.15;1.25/1.25;1.5/1.5;1.75/1.75;1.90/1.90;2.0/2;2.5/2.5;3.0/3"
                        ).split(";");
                        values.forEach((item) => {
                            const parts = item.split("/"),
                                label = parts[0],
                                value = parts[1] || parts[0];
                            this.add(value, label, label);
                        });
                    },
                    onClick: function (value) {
                        editor.focus();
                        editor.fire("saveSnapshot");
                        editor.applyStyle(
                            new window.CKEDITOR.style({
                                element: "span",
                                styles: { "line-height": value },
                            }),
                        );
                        editor.fire("saveSnapshot");
                    },
                });
            },
        });
    }
    function ensureStaffEditor() {
        return new Promise((resolve) => {
            if (staffEditor) return resolve(staffEditor);
            if (!window.CKEDITOR || typeof window.CKEDITOR.replace !== "function") {
                console.error(
                    "CKEditor 4 library was not loaded before modal initialization.",
                );
                showManagerAlert("error", "Rich text editor library could not load.");
                return resolve(null);
            }
            try {
                registerStaffLineHeightPlugin();
                staffEditor = window.CKEDITOR.replace("staffDescription", {
                    height: 260,
                    resize_dir: "vertical",
                    allowedContent: true,
                    fontSize_sizes:
                        "8/8px;9/9px;10/10px;11/11px;12/12px;13/13px;14/14px;15/15px;16/16px;16.3/16.3px;17/17px;18/18px;20/20px;22/22px;24/24px;26/26px;28/28px;30/30px;32/32px;36/36px;40/40px;44/44px;48/48px;54/54px;60/60px;66/66px;72/72px",
                    lineHeight_values:
                        "Normal/normal;1.0/1;1.15/1.15;1.25/1.25;1.5/1.5;1.75/1.75;1.90/1.90;2.0/2;2.5/2.5;3.0/3",
                    extraPlugins: "font,colorbutton,justify,lineheight",
                    removePlugins: "easyimage,cloudservices",
                });
                staffEditor.once("instanceReady", () => resolve(staffEditor));
            } catch (error) {
                staffEditor = null;
                console.error("CKEditor 4 initialization failed:", error);
                showManagerAlert("error", "Rich text editor could not initialize.");
                resolve(null);
            }
        });
    }
    async function branches(selected) {
        const r = await fetch(staffPanel.dataset.branchesUrl, {
            cache: "no-store",
        }),
            j = await r.json();
        department.innerHTML =
            '<option value="">Select department</option>' +
            j.data
                .map(
                    (x) =>
                        `<option value="${x.DepartmentId}">${esc(x.DepartmentName)}</option>`,
                )
                .join("");
        if (selected) department.value = String(selected);
    }
    function showPhoto(url) {
        if (url) {
            preview.src = url;
            preview.classList.remove("d-none");
            prompt.classList.add("d-none");
        } else {
            preview.removeAttribute("src");
            preview.classList.add("d-none");
            prompt.classList.remove("d-none");
        }
    }
    function choosePhoto(f) {
        if (!f) return;
        clearManagerValidation(modalEl);
        if (!["image/jpeg", "image/png", "image/webp"].includes(f.type))
            return showManagerValidation(modalEl, {
                StaffPhoto: "Only JPG, PNG or WEBP images are allowed.",
            });
        if (f.size > 3 * 1024 * 1024)
            return showManagerValidation(modalEl, {
                StaffPhoto: "Photo size cannot exceed 3 MB.",
            });
        selectedPhoto = f;
        showPhoto(URL.createObjectURL(f));
    }
    async function load() {
        try {
            const r = await fetch(staffPanel.dataset.listUrl, { cache: "no-store" }),
                j = await r.json();
            rows = j.data || [];
            const t = body.closest("table");
            if ($.fn.DataTable.isDataTable(t)) $(t).DataTable().destroy();
            body.innerHTML = rows
                .map(
                    (x, i) =>
                        `<tr><td>${i + 1}</td><td>${x.DisplayOrder == null ? "-" : x.DisplayOrder}</td><td><img class="thumb-sm" style="border-radius:50%;width:38px;height:38px" src="${esc(x.PhotoUrl)}" alt="Photo"></td><td>${esc(x.FullName)}</td><td>${esc(x.Email)}</td><td>${esc(x.AlternateEmail || "-")}</td><td>${esc(x.MobileNumber || "-")}</td><td>${esc(x.LandlineNumber || "-")}</td><td>${esc(x.Designation)}</td><td>${esc(x.DepartmentName)}</td><td>${esc(x.Qualification)}</td><td><button type="button" class="btn btn-sm btn-outline-primary rounded-0" data-staff-action="view-description" data-id="${x.StaffId}" ${x.LongDescription ? "" : "disabled"}><i class="bi bi-eye"></i> View</button></td><td><button class="badge-status status-toggle ${x.IsActive ? "badge-active" : "badge-inactive"}" data-staff-action="toggle" data-id="${x.StaffId}">${x.IsActive ? "Active" : "Inactive"}</button></td><td>${esc(x.CreatedBy)}</td><td>${esc(x.UpdatedBy || "-")}</td><td><button class="action-btn edit" data-staff-action="edit" data-id="${x.StaffId}"><i class="bi bi-pencil"></i></button><button class="action-btn del" data-staff-action="delete" data-id="${x.StaffId}"><i class="bi bi-trash"></i></button></td></tr>`,
                )
                .join("");
            initManagerDataTable(t);
        } catch (e) {
            showManagerAlert("error", "Unable to load teaching staff.");
        }
    }
    async function open(x) {
        clearManagerValidation(modalEl);
        selectedPhoto = null;
        photo.value = "";
        id.value = x ? x.StaffId : 0;
        name.value = x ? x.FullName : "";
        email.value = x ? x.Email : "";
        alternateEmail.value = x ? x.AlternateEmail || "" : "";
        mobileNumber.value = x ? x.MobileNumber || "" : "";
        landlineNumber.value = x ? x.LandlineNumber || "" : "";
        designation.value = x ? x.Designation : "";
        qualification.value = x ? x.Qualification : "";
        displayOrder.value = x && x.DisplayOrder != null ? x.DisplayOrder : "";
        status.value = x ? String(x.IsActive) : "true";
        pendingStaffDescription = x && x.LongDescription ? x.LongDescription : "";
        showPhoto(x ? x.PhotoUrl : null);
        document.getElementById("staffModalTitle").textContent = x
            ? "Edit Teaching Staff"
            : "Add Teaching Staff";
        await branches(x ? x.DepartmentId : null);
        modal.show();
    }
    modalEl.addEventListener("shown.bs.modal", async () => {
        const editor = await ensureStaffEditor();
        if (editor) editor.setData(pendingStaffDescription, () => editor.focus());
    });
    document.getElementById("addStaffBtn").onclick = () => open(null);
    drop.onclick = () => photo.click();
    drop.onkeydown = (e) => {
        if (e.key === "Enter" || e.key === " ") photo.click();
    };
    photo.onchange = () => choosePhoto(photo.files[0]);
    ["dragenter", "dragover"].forEach((n) =>
        drop.addEventListener(n, (e) => {
            e.preventDefault();
            drop.classList.add("drag-over");
        }),
    );
    ["dragleave", "drop"].forEach((n) =>
        drop.addEventListener(n, (e) => {
            e.preventDefault();
            drop.classList.remove("drag-over");
        }),
    );
    drop.addEventListener("drop", (e) => choosePhoto(e.dataTransfer.files[0]));
    save.onclick = async () => {
        const e = {};
        if (!name.value.trim()) e.StaffName = "Name is required.";
        if (!email.value.trim()) e.StaffEmail = "Email is required.";
        if (
            alternateEmail.value &&
            !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(alternateEmail.value)
        )
            e.StaffAlternateEmail = "Enter a valid alternate email.";
        if (
            mobileNumber.value &&
            !/^\+?[0-9][0-9\s-]{7,18}$/.test(mobileNumber.value)
        )
            e.StaffMobileNumber = "Enter a valid mobile number.";
        if (
            landlineNumber.value &&
            !/^\+?[0-9][0-9\s()-]{5,23}$/.test(landlineNumber.value)
        )
            e.StaffLandlineNumber = "Enter a valid landline number.";
        if (!designation.value.trim())
            e.StaffDesignation = "Designation is required.";
        if (!department.value) e.StaffDepartment = "Department is required.";
        if (!qualification.value.trim())
            e.StaffQualification = "Qualification is required.";
        if (
            displayOrder.value &&
            (!/^\d+$/.test(displayOrder.value) ||
                +displayOrder.value < 1 ||
                +displayOrder.value > 9999)
        )
            e.StaffDisplayOrder = "Order must be between 1 and 9999.";
        if (+id.value === 0 && !selectedPhoto) e.StaffPhoto = "Photo is required.";
        if (Object.keys(e).length) return showManagerValidation(modalEl, e);
        const editor = await ensureStaffEditor();
        const f = new FormData();
        f.append("__RequestVerificationToken", token);
        f.append("StaffId", id.value);
        f.append("FullName", name.value.trim());
        f.append("Email", email.value.trim());
        f.append("AlternateEmail", alternateEmail.value.trim());
        f.append("MobileNumber", mobileNumber.value.trim());
        f.append("LandlineNumber", landlineNumber.value.trim());
        f.append("Designation", designation.value.trim());
        f.append("DepartmentId", department.value);
        f.append("Qualification", qualification.value.trim());
        if (displayOrder.value) f.append("DisplayOrder", displayOrder.value);
        f.append("LongDescription", editor ? editor.getData() : "");
        f.append("IsActive", status.value);
        if (selectedPhoto) f.append("StaffPhoto", selectedPhoto);
        save.disabled = true;
        try {
            const r = await fetch(staffPanel.dataset.saveUrl, {
                method: "POST",
                body: f,
            });
            const contentType = r.headers.get("content-type") || "";
            if (!contentType.includes("application/json")) {
                console.error(
                    "Teaching Staff API returned a non-JSON response:",
                    await r.text(),
                );
                throw new Error(
                    "Server rejected the submitted content. Please try again.",
                );
            }
            const j = await r.json();
            if (!r.ok || !j.success) {
                showManagerValidation(modalEl, j.errors || {});
                return showManagerAlert(
                    "error",
                    j.message || "Unable to save teaching staff.",
                );
            }
            modal.hide();
            showManagerAlert("success", j.message);
            await load();
        } catch (e) {
            console.error("Teaching Staff save failed:", e);
            showManagerAlert("error", e.message);
        } finally {
            save.disabled = false;
        }
    };
    body.onclick = async (e) => {
        const b = e.target.closest("[data-staff-action]");
        if (!b) return;
        const x = rows.find((r) => r.StaffId === +b.dataset.id);
        if (b.dataset.staffAction === "view-description") {
            document.getElementById("staffDescriptionModalTitle").textContent =
                `${x.FullName} - Long Description`;
            document.getElementById("staffDescriptionFrame").srcdoc =
                `<!doctype html><html><head><meta charset="utf-8"><meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; img-src data: https:"><style>body{font-family:Arial,sans-serif;color:#1c2537;line-height:1.6;padding:24px;margin:0}table{border-collapse:collapse;width:100%}th,td{border:1px solid #dfe4ee;padding:8px}img{max-width:100%;height:auto}a{color:#163a8c}</style></head><body>${x.LongDescription || "<p>No description available.</p>"}</body></html>`;
            descriptionModal.show();
            return;
        }
        if (b.dataset.staffAction === "edit") return open(x);
        if (b.dataset.staffAction === "delete") {
            const c = await Swal.fire({
                title: "Delete staff?",
                icon: "warning",
                showCancelButton: true,
            });
            if (!c.isConfirmed) return;
        }
        const j = await post(
            b.dataset.staffAction === "toggle"
                ? staffPanel.dataset.toggleUrl
                : staffPanel.dataset.deleteUrl,
            { id: b.dataset.id },
        );
        showManagerAlert(j.success ? "success" : "error", j.message);
        if (j.success) await load();
    };
    branches();
    load();
}

// ===== Manage Press Release =====
const pressPanel = document.getElementById("panel-press");
if (pressPanel) {
    const token = pressPanel.querySelector(
        'input[name="__RequestVerificationToken"]',
    ).value,
        body = document.getElementById("pressTableBody"),
        modalEl = document.getElementById("modalPress"),
        modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    const id = document.getElementById("pressId"),
        title = document.getElementById("pressTitle"),
        status = document.getElementById("pressStatus"),
        image = document.getElementById("pressImage"),
        preview = document.getElementById("pressImagePreview"),
        prompt = document.getElementById("pressUploadPrompt"),
        drop = document.getElementById("pressDropZone"),
        save = document.getElementById("savePressBtn");
    let rows = [],
        selectedImage = null;
    const esc = (v) =>
        String(v || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    async function post(url, values) {
        const b = new URLSearchParams(values);
        b.append("__RequestVerificationToken", token);
        const r = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
            },
            body: b.toString(),
        });
        if (!r.ok) throw new Error("Request failed.");
        return r.json();
    }
    function showImage(url) {
        if (url) {
            preview.src = url;
            preview.classList.remove("d-none");
            prompt.classList.add("d-none");
        } else {
            preview.removeAttribute("src");
            preview.classList.add("d-none");
            prompt.classList.remove("d-none");
        }
    }
    function chooseImage(f) {
        if (!f) return;
        clearManagerValidation(modalEl);
        if (!["image/jpeg", "image/png", "image/webp"].includes(f.type))
            return showManagerValidation(modalEl, {
                PressImage: "Only JPG, PNG or WEBP images are allowed.",
            });
        if (f.size > 3 * 1024 * 1024)
            return showManagerValidation(modalEl, {
                PressImage: "Image size cannot exceed 3 MB.",
            });
        selectedImage = f;
        showImage(URL.createObjectURL(f));
    }
    async function load() {
        try {
            const r = await fetch(pressPanel.dataset.listUrl, { cache: "no-store" }),
                j = await r.json();
            rows = j.data || [];
            const t = body.closest("table");
            if ($.fn.DataTable.isDataTable(t)) $(t).DataTable().destroy();
            body.innerHTML = rows
                .map(
                    (x, i) =>
                        `<tr><td>${i + 1}</td><td><img class="thumb-md" src="${esc(x.ImageUrl)}" alt="Press release"></td><td>${esc(x.Title)}</td><td><button class="badge-status status-toggle ${x.IsActive ? "badge-active" : "badge-inactive"}" data-press-action="toggle" data-id="${x.PressReleaseId}">${x.IsActive ? "Active" : "Inactive"}</button></td><td>${esc(x.CreatedBy)}</td><td>${esc(x.UpdatedBy || "-")}</td><td><button class="action-btn edit" data-press-action="edit" data-id="${x.PressReleaseId}"><i class="bi bi-pencil"></i></button><button class="action-btn del" data-press-action="delete" data-id="${x.PressReleaseId}"><i class="bi bi-trash"></i></button></td></tr>`,
                )
                .join("");
            initManagerDataTable(t);
        } catch (e) {
            showManagerAlert("error", "Unable to load press releases.");
        }
    }
    function open(x) {
        clearManagerValidation(modalEl);
        selectedImage = null;
        image.value = "";
        id.value = x ? x.PressReleaseId : 0;
        title.value = x ? x.Title : "";
        status.value = x ? String(x.IsActive) : "true";
        showImage(x ? x.ImageUrl : null);
        document.getElementById("pressModalTitle").textContent = x
            ? "Edit Press Release"
            : "Add Press Release";
        modal.show();
    }
    document.getElementById("addPressBtn").onclick = () => open(null);
    drop.onclick = () => image.click();
    drop.onkeydown = (e) => {
        if (e.key === "Enter" || e.key === " ") image.click();
    };
    image.onchange = () => chooseImage(image.files[0]);
    ["dragenter", "dragover"].forEach((n) =>
        drop.addEventListener(n, (e) => {
            e.preventDefault();
            drop.classList.add("drag-over");
        }),
    );
    ["dragleave", "drop"].forEach((n) =>
        drop.addEventListener(n, (e) => {
            e.preventDefault();
            drop.classList.remove("drag-over");
        }),
    );
    drop.addEventListener("drop", (e) => chooseImage(e.dataTransfer.files[0]));
    save.onclick = async () => {
        const e = {};
        if (!title.value.trim()) e.PressTitle = "Title is required.";
        if (+id.value === 0 && !selectedImage)
            e.PressImage = "Press release image is required.";
        if (Object.keys(e).length) return showManagerValidation(modalEl, e);
        const f = new FormData();
        f.append("__RequestVerificationToken", token);
        f.append("PressReleaseId", id.value);
        f.append("Title", title.value.trim());
        f.append("IsActive", status.value);
        if (selectedImage) f.append("PressImage", selectedImage);
        save.disabled = true;
        try {
            const r = await fetch(pressPanel.dataset.saveUrl, {
                method: "POST",
                body: f,
            }),
                j = await r.json();
            if (!j.success) {
                showManagerValidation(modalEl, j.errors || {});
                return showManagerAlert("error", j.message);
            }
            modal.hide();
            showManagerAlert("success", j.message);
            await load();
        } catch (e) {
            showManagerAlert("error", e.message);
        } finally {
            save.disabled = false;
        }
    };
    body.onclick = async (e) => {
        const b = e.target.closest("[data-press-action]");
        if (!b) return;
        const x = rows.find((r) => r.PressReleaseId === +b.dataset.id);
        if (b.dataset.pressAction === "edit") return open(x);
        if (b.dataset.pressAction === "delete") {
            const c = await Swal.fire({
                title: "Delete press release?",
                icon: "warning",
                showCancelButton: true,
            });
            if (!c.isConfirmed) return;
        }
        const j = await post(
            b.dataset.pressAction === "toggle"
                ? pressPanel.dataset.toggleUrl
                : pressPanel.dataset.deleteUrl,
            { id: b.dataset.id },
        );
        showManagerAlert(j.success ? "success" : "error", j.message);
        if (j.success) await load();
    };
    load();
}

// ===== Academic Calendar =====
const calendarPanel = document.getElementById("panel-academic");
if (calendarPanel) {
    const token = calendarPanel.querySelector(
        'input[name="__RequestVerificationToken"]',
    ).value,
        body = document.getElementById("calendarTableBody"),
        modalEl = document.getElementById("modalCalendar"),
        modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    const id = document.getElementById("calendarId"),
        title = document.getElementById("calendarTitle"),
        semester = document.getElementById("calendarSemester"),
        status = document.getElementById("calendarStatus"),
        file = document.getElementById("calendarFile"),
        prompt = document.getElementById("calendarFilePrompt"),
        drop = document.getElementById("calendarDropZone"),
        save = document.getElementById("saveCalendarBtn");
    let rows = [];
    const esc = (v) =>
        String(v || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    async function post(url, values) {
        const b = new URLSearchParams(values);
        b.append("__RequestVerificationToken", token);
        const r = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
            },
            body: b.toString(),
        });
        if (!r.ok) throw new Error("Request failed.");
        return r.json();
    }
    async function load() {
        try {
            const r = await fetch(calendarPanel.dataset.listUrl, {
                cache: "no-store",
            }),
                j = await r.json();
            rows = j.data || [];
            const t = body.closest("table");
            if ($.fn.DataTable.isDataTable(t)) $(t).DataTable().destroy();
            body.innerHTML = rows
                .map(
                    (x, i) =>
                        `<tr><td>${i + 1}</td><td>${esc(x.Title)}</td><td>${esc(x.SemesterType)}</td><td><a class="btn btn-sm btn-outline-primary rounded-0" href="${esc(x.FileUrl)}" target="_blank">View</a></td><td><button class="badge-status status-toggle ${x.IsActive ? "badge-active" : "badge-inactive"}" data-calendar-action="toggle" data-id="${x.AcademicCalendarId}">${x.IsActive ? "Active" : "Inactive"}</button></td><td>${esc(x.CreatedBy)}</td><td>${esc(x.UpdatedBy || "-")}</td><td><button class="action-btn edit" data-calendar-action="edit" data-id="${x.AcademicCalendarId}"><i class="bi bi-pencil"></i></button><button class="action-btn del" data-calendar-action="delete" data-id="${x.AcademicCalendarId}"><i class="bi bi-trash"></i></button></td></tr>`,
                )
                .join("");
            initManagerDataTable(t);
        } catch (e) {
            showManagerAlert("error", "Unable to load academic calendars.");
        }
    }
    function open(x) {
        clearManagerValidation(modalEl);
        id.value = x ? x.AcademicCalendarId : 0;
        title.value = x ? x.Title : "";
        semester.value = x ? x.SemesterType : "";
        status.value = x ? String(x.IsActive) : "true";
        file.value = "";
        prompt.innerHTML =
            "Click or drag PDF/DOCX file<br><small>Maximum 3 MB; optional while editing</small>";
        document.getElementById("calendarModalTitle").textContent = x
            ? "Edit Academic Calendar"
            : "Add Academic Calendar";
        modal.show();
    }
    document.getElementById("addCalendarBtn").onclick = () => open(null);
    drop.onclick = () => file.click();
    drop.onkeydown = (e) => {
        if (e.key === "Enter" || e.key === " ") file.click();
    };
    file.onchange = () => {
        if (file.files[0]) prompt.textContent = file.files[0].name;
    };
    ["dragenter", "dragover"].forEach((n) =>
        drop.addEventListener(n, (e) => {
            e.preventDefault();
            drop.classList.add("drag-over");
        }),
    );
    ["dragleave", "drop"].forEach((n) =>
        drop.addEventListener(n, (e) => {
            e.preventDefault();
            drop.classList.remove("drag-over");
        }),
    );
    drop.addEventListener("drop", (e) => {
        const f = e.dataTransfer.files[0];
        if (f) {
            const dt = new DataTransfer();
            dt.items.add(f);
            file.files = dt.files;
            prompt.textContent = f.name;
        }
    });
    save.onclick = async () => {
        const e = {};
        if (!title.value.trim()) e.CalendarTitle = "Title is required.";
        if (!semester.value) e.CalendarSemester = "Semester is required.";
        if (+id.value === 0 && !file.files[0])
            e.CalendarFile = "PDF or DOCX file is required.";
        if (Object.keys(e).length) return showManagerValidation(modalEl, e);
        const f = new FormData();
        f.append("__RequestVerificationToken", token);
        f.append("AcademicCalendarId", id.value);
        f.append("Title", title.value.trim());
        f.append("SemesterType", semester.value);
        f.append("IsActive", status.value);
        if (file.files[0]) f.append("CalendarFile", file.files[0]);
        save.disabled = true;
        try {
            const r = await fetch(calendarPanel.dataset.saveUrl, {
                method: "POST",
                body: f,
            }),
                j = await r.json();
            if (!j.success) {
                showManagerValidation(modalEl, j.errors || {});
                return showManagerAlert("error", j.message);
            }
            modal.hide();
            showManagerAlert("success", j.message);
            await load();
        } catch (e) {
            showManagerAlert("error", e.message);
        } finally {
            save.disabled = false;
        }
    };
    body.onclick = async (e) => {
        const b = e.target.closest("[data-calendar-action]");
        if (!b) return;
        const x = rows.find((r) => r.AcademicCalendarId === +b.dataset.id);
        if (b.dataset.calendarAction === "edit") return open(x);
        if (b.dataset.calendarAction === "delete") {
            const c = await Swal.fire({
                title: "Delete academic calendar?",
                icon: "warning",
                showCancelButton: true,
            });
            if (!c.isConfirmed) return;
        }
        const j = await post(
            b.dataset.calendarAction === "toggle"
                ? calendarPanel.dataset.toggleUrl
                : calendarPanel.dataset.deleteUrl,
            { id: b.dataset.id },
        );
        showManagerAlert(j.success ? "success" : "error", j.message);
        if (j.success) await load();
    };
    load();
}

// ===== Manage Manager Users =====
const managerUserPanel = document.getElementById("panel-users");
if (managerUserPanel) {
    const token = managerUserPanel.querySelector(
        'input[name="__RequestVerificationToken"]',
    ).value,
        body = document.getElementById("managerUserTableBody"),
        modalEl = document.getElementById("modalManagerUser"),
        modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    const id = document.getElementById("managerUserId"),
        username = document.getElementById("managerUserUsername"),
        displayName = document.getElementById("managerUserDisplayName"),
        password = document.getElementById("managerUserPassword"),
        confirmPassword = document.getElementById("managerUserConfirmPassword"),
        status = document.getElementById("managerUserStatus"),
        save = document.getElementById("saveManagerUserBtn");
    let rows = [];
    const esc = (v) =>
        String(v || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    async function post(url, values) {
        const b = new URLSearchParams(values);
        b.append("__RequestVerificationToken", token);
        const r = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
            },
            body: b.toString(),
        });
        if (r.status === 401) {
            location.href = "/ManagerAccount/Login";
            throw new Error("Session expired.");
        }
        if (!r.ok) throw new Error("Request failed.");
        return r.json();
    }
    async function load() {
        try {
            const r = await fetch(managerUserPanel.dataset.listUrl, {
                cache: "no-store",
            });
            if (r.status === 401) {
                location.href = "/ManagerAccount/Login";
                return;
            }
            const j = await r.json();
            rows = j.data || [];
            const t = body.closest("table");
            if ($.fn.DataTable.isDataTable(t)) $(t).DataTable().destroy();
            body.innerHTML = rows
                .map(
                    (x, i) =>
                        `<tr><td>${i + 1}</td><td><strong>${esc(x.Username)}</strong>${x.IsCurrent ? ' <span class="badge bg-primary rounded-0">You</span>' : ""}</td><td>${esc(x.DisplayName)}</td><td><span class="badge-status ${x.IsActive ? "badge-active" : "badge-inactive"}">${x.IsActive ? "Active" : "Blocked"}</span></td><td>${x.FailedLoginAttempts}</td><td>${x.IsTemporarilyLocked ? `<span class="badge-status badge-pending">Until ${esc(x.LockoutEndUtc)} UTC</span>` : "-"}</td><td>${esc(x.LastLoginAtUtc || "-")}</td><td>${esc(x.CreatedBy)}</td><td>${esc(x.UpdatedBy || "-")}</td><td><button class="action-btn edit" data-user-action="edit" data-id="${x.ManagerUserId}" title="Edit"><i class="bi bi-pencil"></i></button><button class="action-btn ${x.IsActive ? "del" : "view"}" data-user-action="toggle" data-id="${x.ManagerUserId}" title="${x.IsActive ? "Block" : "Unblock"}" ${x.IsCurrent ? "disabled" : ""}><i class="bi ${x.IsActive ? "bi-lock" : "bi-unlock"}"></i></button>${x.IsTemporarilyLocked ? `<button class="action-btn view" data-user-action="unlock" data-id="${x.ManagerUserId}" title="Remove temporary lock"><i class="bi bi-key"></i></button>` : ""}<button class="action-btn del" data-user-action="delete" data-id="${x.ManagerUserId}" title="Delete" ${x.IsCurrent ? "disabled" : ""}><i class="bi bi-trash"></i></button></td></tr>`,
                )
                .join("");
            initManagerDataTable(t);
        } catch (e) {
            showManagerAlert("error", e.message || "Unable to load users.");
        }
    }
    function open(x) {
        clearManagerValidation(modalEl);
        id.value = x ? x.ManagerUserId : 0;
        username.value = x ? x.Username : "";
        displayName.value = x ? x.DisplayName : "";
        password.value = "";
        confirmPassword.value = "";
        status.value = x ? String(x.IsActive) : "true";
        status.disabled = Boolean(x && x.IsCurrent);
        document.getElementById("managerPasswordHint").textContent = x
            ? "(leave blank to keep current password)"
            : "";
        document.getElementById("managerUserModalTitle").textContent = x
            ? "Edit Manager User"
            : "Add Manager User";
        save.textContent = x ? "Update User" : "Save User";
        modal.show();
    }
    document.getElementById("addManagerUserBtn").onclick = () => open(null);
    save.onclick = async () => {
        const e = {};
        const userPattern = /^[A-Za-z][A-Za-z0-9._-]{3,99}$/;
        if (!userPattern.test(username.value.trim()))
            e.UserUsername = "Enter a valid username (minimum 4 characters).";
        if (!displayName.value.trim())
            e.UserDisplayName = "Display name is required.";
        if (+id.value === 0 || password.value) {
            if (password.value.length < 12)
                e.UserPassword = "Password must contain at least 12 characters.";
            else if (
                !/[A-Z]/.test(password.value) ||
                !/[a-z]/.test(password.value) ||
                !/[0-9]/.test(password.value) ||
                !/[^A-Za-z0-9]/.test(password.value)
            )
                e.UserPassword =
                    "Use uppercase, lowercase, number and special character.";
            if (password.value !== confirmPassword.value)
                e.UserConfirmPassword = "Passwords do not match.";
        }
        if (Object.keys(e).length) return showManagerValidation(modalEl, e);
        save.disabled = true;
        try {
            const j = await post(managerUserPanel.dataset.saveUrl, {
                ManagerUserId: id.value,
                Username: username.value.trim(),
                DisplayName: displayName.value.trim(),
                Password: password.value,
                ConfirmPassword: confirmPassword.value,
                IsActive: status.disabled ? "true" : status.value,
            });
            if (!j.success) {
                showManagerValidation(modalEl, j.errors || {});
                return showManagerAlert("error", j.message);
            }
            modal.hide();
            showManagerAlert("success", j.message);
            await load();
        } catch (e) {
            showManagerAlert("error", e.message);
        } finally {
            save.disabled = false;
        }
    };
    body.onclick = async (e) => {
        const b = e.target.closest("[data-user-action]");
        if (!b || b.disabled) return;
        const x = rows.find((r) => r.ManagerUserId === +b.dataset.id);
        if (b.dataset.userAction === "edit") return open(x);
        let url, message;
        if (b.dataset.userAction === "toggle") {
            url = managerUserPanel.dataset.toggleUrl;
            message = x.IsActive ? "Block this user?" : "Unblock this user?";
        } else if (b.dataset.userAction === "unlock") {
            url = managerUserPanel.dataset.unlockUrl;
            message = "Remove temporary login lock?";
        } else {
            url = managerUserPanel.dataset.deleteUrl;
            message = "Permanently delete this user?";
        }
        const c = await Swal.fire({
            title: message,
            text: x.Username,
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#e0453f",
            confirmButtonText: "Yes, continue",
        });
        if (!c.isConfirmed) return;
        try {
            const j = await post(url, { id: x.ManagerUserId });
            showManagerAlert(j.success ? "success" : "error", j.message);
            if (j.success) await load();
        } catch (e) {
            showManagerAlert("error", e.message);
        }
    };
    load();
}
