/*global
    Vue
*/

// Compatibility replacement for the abandoned Vue-2-only vuejs-datepicker.
// It keeps the existing component contract while using the browser date input.
var vuejsDatepicker = {
    props: {
        value: null,
        modelValue: null,
        format: String,
        mondayFirst: Boolean,
        typeable: Boolean,
        bootstrapStyling: Boolean
    },
    emits: ["input", "update:modelValue", "closed"],
    computed: {
        inputValue: function () {
            var value = this.modelValue !== undefined ? this.modelValue : this.value;

            if (!value) {
                return "";
            }

            if (value instanceof Date) {
                return value.getFullYear() + "-" +
                    String(value.getMonth() + 1).padStart(2, "0") + "-" +
                    String(value.getDate()).padStart(2, "0");
            }

            return String(value).substring(0, 10);
        }
    },
    methods: {
        update: function (event) {
            var value = event.target.value;
            var date = value ? new Date(value + "T00:00:00") : null;

            this.$emit("input", date);
            this.$emit("update:modelValue", date);
            this.$emit("closed", date);
        }
    },
    template: "<div class=\"vdp-datepicker\"><input type=\"date\" class=\"form-control\" :value=\"inputValue\" @change=\"update\"></div>"
};

Vue.component("datepicker", vuejsDatepicker);
