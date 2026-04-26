$(document).ready(function () {
    const cartBadges = $(".cart-count");

    if (!cartBadges.length) {
        return;
    }

    $.ajax({
        url: "/Cart/GetCartCount",
        type: "GET",
        success: function (res) {
            cartBadges.text(res.count ?? 0);
        },
        error: function () {
            cartBadges.text("0");
        }
    });
});
