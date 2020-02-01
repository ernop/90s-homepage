$("button.publish").click(function () {
    var articleId = $(this).attr("articleId");
    var url = "/ajax/article/publishtoggle/" + articleId;
    $.ajax({
        url: url,
        method: "POST",
        dataType: "json"
    }).done(function (res) {
        var target = $("button.publish[articleId=" + articleId + "]");
        if (res.status) {
            $(target).text("Unpublish");
        }
        else {
            $(target).text("Publish");
        }
    })
});

$("button.delete").click(function () {
    var articleId = $(this).attr("articleId");
    var url = "/ajax/article/deletetoggle/" + articleId;
    $.ajax({
        url: url,
        method: "POST",
        dataType: "json"
    }).done(function (res) {
        var target = $("button.delete[articleId=" + articleId + "]");
        if (res.status) {
            $(target).text("Undelete");
        }
        else {
            $(target).text("Delete");
        }
    })
});

//hacked article edit button
var isViewArticle = $(".pageData[kind=viewArticle]");
if (isViewArticle.length > 0) {
    document.onkeyup = function (e) {
        if (e.which == 69) {
            var title = $(".pageData").attr("title");
            document.location = "/article/edit/" + title;
        }
    };

    
};