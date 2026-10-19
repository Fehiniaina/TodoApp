window.todoApp = window.todoApp || {};
window.todoApp.getTimeZone = () => Intl.DateTimeFormat().resolvedOptions().timeZone;