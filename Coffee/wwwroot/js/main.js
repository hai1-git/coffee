(function ($) {
    "use strict";
    
    // Desktop hover dropdown without the flicker caused by click toggling.
    $(document).ready(function () {
        function toggleNavbarMethod() {
            var $dropdowns = $('.navbar .dropdown');

            $dropdowns.off('.coffeeHover');

            if ($(window).width() > 992) {
                $dropdowns.on('mouseenter.coffeeHover', function () {
                    var $dropdown = $(this);

                    $dropdown.addClass('show');
                    $dropdown.find('.dropdown-toggle').attr('aria-expanded', 'true');
                    $dropdown.find('.dropdown-menu').addClass('show');
                }).on('mouseleave.coffeeHover', function () {
                    var $dropdown = $(this);

                    $dropdown.removeClass('show');
                    $dropdown.find('.dropdown-toggle').attr('aria-expanded', 'false');
                    $dropdown.find('.dropdown-menu').removeClass('show');
                });
            } else {
                $dropdowns.removeClass('show');
                $dropdowns.find('.dropdown-menu').removeClass('show');
                $dropdowns.find('.dropdown-toggle').attr('aria-expanded', 'false');
            }
        }

        toggleNavbarMethod();
        $(window).resize(toggleNavbarMethod);
    });
    
    
    // Back to top button
    $(window).scroll(function () {
        if ($(this).scrollTop() > 100) {
            $('.back-to-top').fadeIn('slow');
        } else {
            $('.back-to-top').fadeOut('slow');
        }
    });
    $('.back-to-top').click(function () {
        $('html, body').animate({scrollTop: 0}, 1500, 'easeInOutExpo');
        return false;
    });
    

    // Date and time picker
    $('.date').datetimepicker({
        format: 'L'
    });
    $('.time').datetimepicker({
        format: 'LT'
    });


    // Testimonials carousel
    $(".testimonial-carousel").owlCarousel({
        autoplay: true,
        smartSpeed: 1500,
        margin: 30,
        dots: true,
        loop: true,
        center: true,
        responsive: {
            0:{
                items:1
            },
            576:{
                items:1
            },
            768:{
                items:2
            },
            992:{
                items:3
            }
        }
    });

    function syncCoffeeHeader() {
        $('.coffee-topbar').toggleClass('coffee-topbar-scrolled', $(window).scrollTop() > 24);
    }

    syncCoffeeHeader();
    $(window).on('scroll', syncCoffeeHeader);
    
})(jQuery);

