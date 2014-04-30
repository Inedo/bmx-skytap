function BmSkytapResourcePicker(o) {
    /// <param name="o" value="{
    /// ajaxUrl: '',
    /// token: '',
    /// selector: ''
    /// }"/>

    var createSearchChoice = function (term) {
        return {
            id: encodeURIComponent(term) + '&' + encodeURIComponent(term),
            text: term
        };
    };

    var initSelection = function (element, callback) {
        var val = element.val();
        if (val) {
            var parts = val.split('&');
            callback({
                id: decodeURIComponent(parts[0]),
                text: decodeURIComponent(parts[1])
            });
        }
    };

    if (o.token) {
        $(o.selector).select2({
            createSearchChoice: createSearchChoice,
            initSelection: initSelection,
            ajax: {
                type: 'POST',
                url: o.ajaxUrl,
                data: function () {
                    return {
                        token: o.token
                    };
                },
                results: function (data) {
                    return {
                        results: data
                    };
                }
            },
            formatSearching: function () {
                return 'Getting list from Skytap...';
            }
        });
    } else {
        $(o.selector).select2({
            createSearchChoice: createSearchChoice,
            initSelection: initSelection,
            data: []
        });
    }
}