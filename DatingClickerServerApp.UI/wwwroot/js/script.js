function scrollToBottom(containerId) {
    var container = document.getElementById(containerId);
    container.scrollTop = container.scrollHeight;
};

function scrollToTop() {
    window.scrollTo(0, 0);
}

function showUserModal() {
    const modal = new bootstrap.Modal(document.getElementById('userModal'));
    modal.show();

    history.pushState({ modalOpened: true }, 'Modal Open', document.location + '#modal');

    modal._element.addEventListener('hidden.bs.modal', handleHideModal);
}

function handleHideModal() {
    if (history.state.modalOpened) {
        history.go(-1);
    }
}

function handlePopState() {
    const modal = bootstrap.Modal.getInstance(document.getElementById('userModal'));

    if (modal) {
        modal.hide();
    }
}

window.addEventListener('popstate', handlePopState);

window.addEventListener('beforeunload', () => {
    window.removeEventListener('popstate', handlePopState);
});