
function checkNumberOnly(e) {
    -1!==$.inArray(e.keyCode,[46,8,9,27,13,190])||/65|67|86|88/.test(e.keyCode)&&(!0===e.ctrlKey||!0===e.metaKey)||35<=e.keyCode&&40>=e.keyCode||(e.shiftKey||48>e.keyCode||57<e.keyCode)&&(96>e.keyCode||105<e.keyCode)&&e.preventDefault()
}


/*
 * Form Validation
 */
 $(function () {
    $("#KavDiscountForm").validate({
        rules: {
            username: {
                required: true
            }
        },
        //For custom messages
        messages: {
            username: {
                required: "kullanıcı Adınızı gerekli"
            }
        },
        errorElement: 'div',
        errorPlacement: function (error, element) {
            var placement = $(element).data('error');
            if (placement) {
                $(placement).append(error)
            } else {
                error.insertAfter(element);
            }
        }
    });
});


function blockSpecialChar(e) {
    var k;
    document.all ? k = e.keyCode : k = e.which;
    return ((k > 64 && k < 91) || (k > 96 && k < 123) || k == 8 || k == 32 || (k >= 48 && k <= 57));
} 