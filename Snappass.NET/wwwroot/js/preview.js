(function () {
    $('#revealSecret').click(function (e) {
        $('#revealed').show();
        $('#revealSecret').prop("disabled", true);
    });
})();