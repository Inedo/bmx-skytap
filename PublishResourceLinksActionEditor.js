function BmPublishResourceLinksActionEditor(o) {
    /// <param name="o" value="{
    /// vmDataSelector:'',
    /// containerSelector:''
    /// }"/>

    function VmViewModel(name, access) {
        this.name = ko.observable(name);
        this.access = ko.observable(access);
    }

    function PublishViewModel(initialData) {
        /// <param name="initialData" type="String"/>

        var self = this;

        this.defaultAccess = ko.observable('view_only');

        this.vms = ko.observableArray();

        this.vmData = ko.computed(function () {
            var data = encodeURIComponent(self.defaultAccess());
            if (self.vms().length > 0) {
                for (var i = 0; i < self.vms().length; i++) {
                    data += '&';
                    var vm = self.vms()[i];
                    data += encodeURIComponent(vm.name()) + '&' + encodeURIComponent(vm.access());
                }
            }
            return data;
        });

        this.deleteVm = function (vm) {
            self.vms.remove(vm);
        };

        this.addVm = function () {
            self.vms.push(new VmViewModel('', 'view_only'));
        };

        var initialDataParts = initialData.split('&');
        if (initialDataParts.length > 0) {
            self.defaultAccess(initialDataParts[0]);
            for (var i = 1; i < initialDataParts.length - 1; i += 2)
                self.vms.push(new VmViewModel(decodeURIComponent(initialDataParts[i]), decodeURIComponent(initialDataParts[i + 1])));
        }
    }

    var publishVm = new PublishViewModel($(o.vmDataSelector).val());

    publishVm.vmData.subscribe(function (value) {
        $(o.vmDataSelector).val(value);
    });

    ko.applyBindings(publishVm, $(o.containerSelector)[0]);
}