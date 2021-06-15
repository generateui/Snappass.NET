(function () {
    $('#revealSecret').click(function (e) {
        var form = $('<form/>')
          .attr('id', 'revealSecretForm')
          .attr('method', 'post');
        form.appendTo($('body'));
        form.submit();
    });
})();