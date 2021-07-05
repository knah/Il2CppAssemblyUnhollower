using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime
{
    internal unsafe static class Il2CppApi
    {
        #region IL2CPP Functions

        public static void il2cpp_init(IntPtr domain_name) => IL2CPP.il2cpp_init(domain_name);

        public static void il2cpp_init_utf16(IntPtr domain_name) => IL2CPP.il2cpp_init_utf16(domain_name);

        public static void il2cpp_shutdown() => IL2CPP.il2cpp_shutdown();

        public static void il2cpp_set_config_dir(IntPtr config_path) => IL2CPP.il2cpp_set_config_dir(config_path);

        public static void il2cpp_set_data_dir(IntPtr data_path) => IL2CPP.il2cpp_set_data_dir(data_path);

        public static void il2cpp_set_temp_dir(IntPtr temp_path) => IL2CPP.il2cpp_set_temp_dir(temp_path);

        public static void il2cpp_set_commandline_arguments(int argc, IntPtr argv, IntPtr basedir) => IL2CPP.il2cpp_set_commandline_arguments(argc, argv, basedir);

        public static void il2cpp_set_commandline_arguments_utf16(int argc, IntPtr argv, IntPtr basedir) => IL2CPP.il2cpp_set_commandline_arguments_utf16(argc, argv, basedir);

        public static void il2cpp_set_config_utf16(IntPtr executablePath) => IL2CPP.il2cpp_set_config_utf16(executablePath);

        public static void il2cpp_set_config(IntPtr executablePath) => IL2CPP.il2cpp_set_config(executablePath);

        public static void il2cpp_set_memory_callbacks(IntPtr callbacks) => IL2CPP.il2cpp_set_memory_callbacks(callbacks);

        public static IntPtr il2cpp_get_corlib() => IL2CPP.il2cpp_get_corlib();

        public static void il2cpp_add_internal_call(IntPtr name, IntPtr method) => IL2CPP.il2cpp_add_internal_call(name, method);

        public static IntPtr il2cpp_resolve_icall([MarshalAs(UnmanagedType.LPStr)] string name) => IL2CPP.il2cpp_resolve_icall(name);

        public static IntPtr il2cpp_alloc(uint size) => IL2CPP.il2cpp_alloc(size);

        public static void il2cpp_free(IntPtr ptr) => IL2CPP.il2cpp_free(ptr);

        #endregion

        #region Arrays

        public static IntPtr il2cpp_array_class_get(IntPtr element_class, uint rank) => IL2CPP.il2cpp_array_class_get(element_class, rank);

        public static uint il2cpp_array_length(IntPtr array) => IL2CPP.il2cpp_array_length(array);

        public static uint il2cpp_array_get_byte_length(IntPtr array) => IL2CPP.il2cpp_array_get_byte_length(array);

        public static IntPtr il2cpp_array_new(IntPtr elementTypeInfo, ulong length) => IL2CPP.il2cpp_array_new(elementTypeInfo, length);

        public static IntPtr il2cpp_array_new_specific(IntPtr arrayTypeInfo, ulong length) => IL2CPP.il2cpp_array_new_specific(arrayTypeInfo, length);

        public static IntPtr il2cpp_array_new_full(IntPtr array_class, ref ulong lengths, ref ulong lower_bounds) => IL2CPP.il2cpp_array_new_full(array_class, ref lengths, ref lower_bounds);

        public static IntPtr il2cpp_bounded_array_class_get(IntPtr element_class, uint rank, bool bounded) => IL2CPP.il2cpp_bounded_array_class_get(element_class, rank, bounded);

        public static int il2cpp_array_element_size(IntPtr array_class) => IL2CPP.il2cpp_array_element_size(array_class);

        #endregion

        #region Assemblies

        public static IntPtr il2cpp_assembly_get_image(IntPtr assembly) => IL2CPP.il2cpp_assembly_get_image(assembly);

        public static IntPtr il2cpp_assembly_get_name(IntPtr assembly) => UnityVersionHandler.assemblyStructHandler.il2cpp_assembly_get_name(assembly);

        #endregion

        #region Classes

        public static IntPtr il2cpp_class_enum_basetype(IntPtr klass) => IL2CPP.il2cpp_class_enum_basetype(klass);

        public static bool il2cpp_class_is_generic(IntPtr klass) => IL2CPP.il2cpp_class_is_generic(klass);

        public static bool il2cpp_class_is_inflated(IntPtr klass) => IL2CPP.il2cpp_class_is_inflated(klass);

        public static bool il2cpp_class_is_assignable_from(IntPtr klass, IntPtr oklass) => IL2CPP.il2cpp_class_is_assignable_from(klass, oklass);

        public static bool il2cpp_class_is_subclass_of(IntPtr klass, IntPtr klassc, bool check_interfaces) => IL2CPP.il2cpp_class_is_subclass_of(klass, klassc, check_interfaces);

        public static bool il2cpp_class_has_parent(IntPtr klass, IntPtr klassc) => IL2CPP.il2cpp_class_has_parent(klass, klassc);

        public static IntPtr il2cpp_class_from_il2cpp_type(IntPtr type) => IL2CPP.il2cpp_class_from_il2cpp_type(type);

        public static IntPtr il2cpp_class_from_name(IntPtr image, [MarshalAs(UnmanagedType.LPStr)] string namespaze, [MarshalAs(UnmanagedType.LPStr)] string name) => IL2CPP.il2cpp_class_from_name(image, namespaze, name);

        public static IntPtr il2cpp_class_from_system_type(IntPtr type) => IL2CPP.il2cpp_class_from_system_type(type);

        public static IntPtr il2cpp_class_get_element_class(IntPtr klass) => IL2CPP.il2cpp_class_get_element_class(klass);

        public static IntPtr il2cpp_class_get_events(IntPtr klass, ref IntPtr iter) => IL2CPP.il2cpp_class_get_events(klass, ref iter);

        public static IntPtr il2cpp_class_get_fields(IntPtr klass, ref IntPtr iter) => IL2CPP.il2cpp_class_get_fields(klass, ref iter);

        public static IntPtr il2cpp_class_get_nested_types(IntPtr klass, ref IntPtr iter) => IL2CPP.il2cpp_class_get_nested_types(klass, ref iter);

        public static IntPtr il2cpp_class_get_interfaces(IntPtr klass, ref IntPtr iter) => IL2CPP.il2cpp_class_get_interfaces(klass, ref iter);

        public static IntPtr il2cpp_class_get_properties(IntPtr klass, ref IntPtr iter) => IL2CPP.il2cpp_class_get_properties(klass, ref iter);

        public static IntPtr il2cpp_class_get_property_from_name(IntPtr klass, IntPtr name) => IL2CPP.il2cpp_class_get_property_from_name(klass, name);

        public static IntPtr il2cpp_class_get_field_from_name(IntPtr klass, [MarshalAs(UnmanagedType.LPStr)] string name) => IL2CPP.il2cpp_class_get_field_from_name(klass, name);

        public static IntPtr il2cpp_class_get_methods(IntPtr klass, ref IntPtr iter) => IL2CPP.il2cpp_class_get_methods(klass, ref iter);

        public static IntPtr il2cpp_class_get_method_from_name(IntPtr klass, [MarshalAs(UnmanagedType.LPStr)] string name, int argsCount) => IL2CPP.il2cpp_class_get_method_from_name(klass, name, argsCount);

        public static IntPtr il2cpp_class_get_name(IntPtr klass) => IL2CPP.il2cpp_class_get_name(klass);

        public static IntPtr il2cpp_class_get_namespace(IntPtr klass) => IL2CPP.il2cpp_class_get_namespace(klass);

        public static IntPtr il2cpp_class_get_parent(IntPtr klass) => IL2CPP.il2cpp_class_get_parent(klass);

        public static IntPtr il2cpp_class_get_declaring_type(IntPtr klass) => IL2CPP.il2cpp_class_get_declaring_type(klass);

        public static int il2cpp_class_instance_size(IntPtr klass) => IL2CPP.il2cpp_class_instance_size(klass);

        public static uint il2cpp_class_num_fields(IntPtr enumKlass) => IL2CPP.il2cpp_class_num_fields(enumKlass);

        public static bool il2cpp_class_is_valuetype(IntPtr klass) => IL2CPP.il2cpp_class_is_valuetype(klass);

        public static int il2cpp_class_value_size(IntPtr klass, ref uint align) => IL2CPP.il2cpp_class_value_size(klass, ref align);

        public static bool il2cpp_class_is_blittable(IntPtr klass) => IL2CPP.il2cpp_class_is_blittable(klass);

        public static int il2cpp_class_get_flags(IntPtr klass) => IL2CPP.il2cpp_class_get_flags(klass);

        public static bool il2cpp_class_is_abstract(IntPtr klass) => IL2CPP.il2cpp_class_is_abstract(klass);

        public static bool il2cpp_class_is_interface(IntPtr klass) => IL2CPP.il2cpp_class_is_interface(klass);

        public static int il2cpp_class_array_element_size(IntPtr klass) => IL2CPP.il2cpp_class_array_element_size(klass);

        public static IntPtr il2cpp_class_from_type(IntPtr type) => IL2CPP.il2cpp_class_from_type(type);

        public static IntPtr il2cpp_class_get_type(IntPtr klass) => IL2CPP.il2cpp_class_get_type(klass);

        public static uint il2cpp_class_get_type_token(IntPtr klass) => IL2CPP.il2cpp_class_get_type_token(klass);

        public static bool il2cpp_class_has_attribute(IntPtr klass, IntPtr attr_class) => IL2CPP.il2cpp_class_has_attribute(klass, attr_class);

        public static bool il2cpp_class_has_references(IntPtr klass) => IL2CPP.il2cpp_class_has_references(klass);

        public static bool il2cpp_class_is_enum(IntPtr klass) => IL2CPP.il2cpp_class_is_enum(klass);

        public static IntPtr il2cpp_class_get_image(IntPtr klass) => IL2CPP.il2cpp_class_get_image(klass);

        public static IntPtr il2cpp_class_get_assemblyname(IntPtr klass) => IL2CPP.il2cpp_class_get_assemblyname(klass);

        public static int il2cpp_class_get_rank(IntPtr klass) => IL2CPP.il2cpp_class_get_rank(klass);

        public static uint il2cpp_class_get_bitmap_size(IntPtr klass) => IL2CPP.il2cpp_class_get_bitmap_size(klass);

        public static void il2cpp_class_get_bitmap(IntPtr klass, ref uint bitmap) => IL2CPP.il2cpp_class_get_bitmap(klass, ref bitmap);

        #endregion

        #region Custom Attributes

        public static IntPtr il2cpp_custom_attrs_from_class(IntPtr klass) => IL2CPP.il2cpp_custom_attrs_from_class(klass);

        public static IntPtr il2cpp_custom_attrs_from_method(IntPtr method) => IL2CPP.il2cpp_custom_attrs_from_method(method);

        public static IntPtr il2cpp_custom_attrs_get_attr(IntPtr ainfo, IntPtr attr_klass) => IL2CPP.il2cpp_custom_attrs_get_attr(ainfo, attr_klass);

        public static bool il2cpp_custom_attrs_has_attr(IntPtr ainfo, IntPtr attr_klass) => IL2CPP.il2cpp_custom_attrs_has_attr(ainfo, attr_klass);

        public static IntPtr il2cpp_custom_attrs_construct(IntPtr cinfo) => IL2CPP.il2cpp_custom_attrs_construct(cinfo);

        public static void il2cpp_custom_attrs_free(IntPtr ainfo) => IL2CPP.il2cpp_custom_attrs_free(ainfo);

        #endregion

        #region Debugging

        public static void il2cpp_set_find_plugin_callback(IntPtr method) => IL2CPP.il2cpp_set_find_plugin_callback(method);

        public static void il2cpp_register_log_callback(IntPtr method) => IL2CPP.il2cpp_register_log_callback(method);

        public static void il2cpp_debugger_set_agent_options(IntPtr options) => IL2CPP.il2cpp_debugger_set_agent_options(options);

        public static bool il2cpp_is_debugger_attached() => IL2CPP.il2cpp_is_debugger_attached();

        public static unsafe void il2cpp_unity_install_unitytls_interface(void* unitytlsInterfaceStruct) => IL2CPP.il2cpp_unity_install_unitytls_interface(unitytlsInterfaceStruct);

        #endregion

        #region Domains

        public static IntPtr il2cpp_domain_get() => IL2CPP.il2cpp_domain_get();

        public static IntPtr il2cpp_domain_assembly_open(IntPtr domain, IntPtr name) => IL2CPP.il2cpp_domain_assembly_open(domain, name);

        public static IntPtr* il2cpp_domain_get_assemblies(IntPtr domain, ref uint size) => IL2CPP.il2cpp_domain_get_assemblies(domain, ref size);

        #endregion

        #region Exceptions

        public static IntPtr il2cpp_exception_from_name_msg(IntPtr image, IntPtr name_space, IntPtr name, IntPtr msg) => IL2CPP.il2cpp_exception_from_name_msg(image, name_space, name, msg);

        public static IntPtr il2cpp_get_exception_argument_null(IntPtr arg) => IL2CPP.il2cpp_get_exception_argument_null(arg);

        public static void il2cpp_format_exception(IntPtr ex, void* message, int message_size) => IL2CPP.il2cpp_format_exception(ex, message, message_size);

        public static void il2cpp_format_stack_trace(IntPtr ex, void* output, int output_size) => IL2CPP.il2cpp_format_stack_trace(ex, output, output_size);

        public static void il2cpp_unhandled_exception(IntPtr ex) => IL2CPP.il2cpp_unhandled_exception(ex);

        #endregion

        #region Fields

        public static int il2cpp_field_get_flags(IntPtr field) => IL2CPP.il2cpp_field_get_flags(field);

        public static IntPtr il2cpp_field_get_name(IntPtr field) => UnityVersionHandler.fieldInfoStructHandler.il2cpp_field_get_name(field);

        public static IntPtr il2cpp_field_get_parent(IntPtr field) => UnityVersionHandler.fieldInfoStructHandler.il2cpp_field_get_parent(field);

        public static uint il2cpp_field_get_offset(IntPtr field) => UnityVersionHandler.fieldInfoStructHandler.il2cpp_field_get_offset(field);

        public static IntPtr il2cpp_field_get_type(IntPtr field) => UnityVersionHandler.fieldInfoStructHandler.il2cpp_field_get_type(field);

        public static void il2cpp_field_get_value(IntPtr obj, IntPtr field, void* value) => IL2CPP.il2cpp_field_get_value(obj, field, value);

        public static IntPtr il2cpp_field_get_value_object(IntPtr field, IntPtr obj) => IL2CPP.il2cpp_field_get_value_object(field, obj);

        public static bool il2cpp_field_has_attribute(IntPtr field, IntPtr attr_class) => IL2CPP.il2cpp_field_has_attribute(field, attr_class);

        public static void il2cpp_field_set_value(IntPtr obj, IntPtr field, void* value) => IL2CPP.il2cpp_field_set_value(obj, field, value);

        public static void il2cpp_field_static_get_value(IntPtr field, void* value) => IL2CPP.il2cpp_field_static_get_value(field, value);

        public static void il2cpp_field_static_set_value(IntPtr field, void* value) => IL2CPP.il2cpp_field_static_set_value(field, value);

        public static void il2cpp_field_set_value_object(IntPtr instance, IntPtr field, IntPtr value) => IL2CPP.il2cpp_field_set_value_object(instance, field, value);

        #endregion

        #region Garbage Collector

        public static void il2cpp_gc_collect(int maxGenerations) => IL2CPP.il2cpp_gc_collect(maxGenerations);

        public static int il2cpp_gc_collect_a_little() => IL2CPP.il2cpp_gc_collect_a_little();

        public static void il2cpp_gc_disable() => IL2CPP.il2cpp_gc_disable();

        public static void il2cpp_gc_enable() => IL2CPP.il2cpp_gc_enable();

        public static bool il2cpp_gc_is_disabled() => IL2CPP.il2cpp_gc_is_disabled();

        public static long il2cpp_gc_get_used_size() => IL2CPP.il2cpp_gc_get_used_size();

        public static long il2cpp_gc_get_heap_size() => IL2CPP.il2cpp_gc_get_heap_size();

        public static void il2cpp_gc_wbarrier_set_field(IntPtr obj, IntPtr targetAddress, IntPtr gcObj) => IL2CPP.il2cpp_gc_wbarrier_set_field(obj, targetAddress, gcObj);

        #endregion

        #region GC Handles

        public static uint il2cpp_gchandle_new(IntPtr obj, bool pinned) => IL2CPP.il2cpp_gchandle_new(obj, pinned);

        public static uint il2cpp_gchandle_new_weakref(IntPtr obj, bool track_resurrection) => IL2CPP.il2cpp_gchandle_new_weakref(obj, track_resurrection);

        public static IntPtr il2cpp_gchandle_get_target(uint gchandle) => IL2CPP.il2cpp_gchandle_get_target(gchandle);

        public static void il2cpp_gchandle_free(uint gchandle) => IL2CPP.il2cpp_gchandle_free(gchandle);

        #endregion

        #region Images

        public static IntPtr il2cpp_image_get_assembly(IntPtr image) => IL2CPP.il2cpp_image_get_assembly(image);

        public static IntPtr il2cpp_image_get_name(IntPtr image) => IL2CPP.il2cpp_image_get_name(image);

        public static IntPtr il2cpp_image_get_filename(IntPtr image) => IL2CPP.il2cpp_image_get_filename(image);

        public static IntPtr il2cpp_image_get_entry_point(IntPtr image) => IL2CPP.il2cpp_image_get_entry_point(image);

        public static uint il2cpp_image_get_class_count(IntPtr image) => IL2CPP.il2cpp_image_get_class_count(image);

        public static IntPtr il2cpp_image_get_class(IntPtr image, uint index) => IL2CPP.il2cpp_image_get_class(image, index);

        #endregion

        #region Memory

        public static IntPtr il2cpp_capture_memory_snapshot() => IL2CPP.il2cpp_capture_memory_snapshot();

        public static void il2cpp_free_captured_memory_snapshot(IntPtr snapshot) => IL2CPP.il2cpp_free_captured_memory_snapshot(snapshot);

        #endregion

        #region Methods

        public static IntPtr il2cpp_method_get_return_type(IntPtr method) => UnityVersionHandler.methodInfoStructHandler.il2cpp_method_get_return_type(method);

        public static IntPtr il2cpp_method_get_declaring_type(IntPtr method) => IL2CPP.il2cpp_method_get_declaring_type(method);

        public static IntPtr il2cpp_method_get_name(IntPtr method) => UnityVersionHandler.methodInfoStructHandler.il2cpp_method_get_name(method);

        public static IntPtr il2cpp_method_get_from_reflection(IntPtr method) => IL2CPP.il2cpp_method_get_from_reflection(method);

        public static IntPtr il2cpp_method_get_object(IntPtr method, IntPtr refclass) => IL2CPP.il2cpp_method_get_object(method, refclass);

        public static bool il2cpp_method_is_generic(IntPtr method) => IL2CPP.il2cpp_method_is_generic(method);

        public static bool il2cpp_method_is_inflated(IntPtr method) => IL2CPP.il2cpp_method_is_inflated(method);

        public static bool il2cpp_method_is_instance(IntPtr method) => IL2CPP.il2cpp_method_is_instance(method);

        public static uint il2cpp_method_get_param_count(IntPtr method) => UnityVersionHandler.methodInfoStructHandler.il2cpp_method_get_param_count(method);

        public static IntPtr il2cpp_method_get_param(IntPtr method, uint index) => IL2CPP.il2cpp_method_get_param(method, index);

        public static IntPtr il2cpp_method_get_class(IntPtr method) => UnityVersionHandler.methodInfoStructHandler.il2cpp_method_get_class(method);

        public static bool il2cpp_method_has_attribute(IntPtr method, IntPtr attr_class) => IL2CPP.il2cpp_method_has_attribute(method, attr_class);

        public static uint il2cpp_method_get_flags(IntPtr method, ref uint iflags) => IL2CPP.il2cpp_method_get_flags(method, ref iflags);

        public static uint il2cpp_method_get_token(IntPtr method) => UnityVersionHandler.methodInfoStructHandler.il2cpp_method_get_token(method);

        public static IntPtr il2cpp_method_get_param_name(IntPtr method, uint index) => IL2CPP.il2cpp_method_get_param_name(method, index);

        #endregion

        #region Monitors

        public static void il2cpp_monitor_enter(IntPtr obj) => IL2CPP.il2cpp_monitor_enter(obj);

        public static bool il2cpp_monitor_try_enter(IntPtr obj, uint timeout) => IL2CPP.il2cpp_monitor_try_enter(obj, timeout);

        public static void il2cpp_monitor_exit(IntPtr obj) => IL2CPP.il2cpp_monitor_exit(obj);

        public static void il2cpp_monitor_pulse(IntPtr obj) => IL2CPP.il2cpp_monitor_pulse(obj);

        public static void il2cpp_monitor_pulse_all(IntPtr obj) => IL2CPP.il2cpp_monitor_pulse_all(obj);

        public static void il2cpp_monitor_wait(IntPtr obj) => IL2CPP.il2cpp_monitor_wait(obj);

        public static bool il2cpp_monitor_try_wait(IntPtr obj, uint timeout) => IL2CPP.il2cpp_monitor_try_wait(obj, timeout);

        #endregion

        #region Objects

        public static IntPtr il2cpp_object_get_class(IntPtr obj) => IL2CPP.il2cpp_object_get_class(obj);

        public static uint il2cpp_object_get_size(IntPtr obj) => IL2CPP.il2cpp_object_get_size(obj);

        public static IntPtr il2cpp_object_get_virtual_method(IntPtr obj, IntPtr method) => IL2CPP.il2cpp_object_get_virtual_method(obj, method);

        public static IntPtr il2cpp_object_new(IntPtr klass) => IL2CPP.il2cpp_object_new(klass);

        public static IntPtr il2cpp_object_unbox(IntPtr obj) => IL2CPP.il2cpp_object_unbox(obj);

        public static IntPtr il2cpp_value_box(IntPtr klass, IntPtr data) => IL2CPP.il2cpp_value_box(klass, data);

        #endregion

        #region Profilers

        public static void il2cpp_profiler_install(IntPtr prof, IntPtr shutdown_callback) => IL2CPP.il2cpp_profiler_install(prof, shutdown_callback);

        // public static void il2cpp_profiler_set_events(IL2CPP_ProfileFlags events) => IL2CPP.il2cpp_profiler_set_events(events);

        public static void il2cpp_profiler_install_enter_leave(IntPtr enter, IntPtr leave) => IL2CPP.il2cpp_profiler_install_enter_leave(enter, leave);

        public static void il2cpp_profiler_install_allocation(IntPtr callback) => IL2CPP.il2cpp_profiler_install_allocation(callback);

        public static void il2cpp_profiler_install_gc(IntPtr callback, IntPtr heap_resize_callback) => IL2CPP.il2cpp_profiler_install_gc(callback, heap_resize_callback);

        public static void il2cpp_profiler_install_fileio(IntPtr callback) => IL2CPP.il2cpp_profiler_install_fileio(callback);

        public static void il2cpp_profiler_install_thread(IntPtr start, IntPtr end) => IL2CPP.il2cpp_profiler_install_thread(start, end);

        #endregion

        #region Properties

        public static uint il2cpp_property_get_flags(IntPtr prop) => IL2CPP.il2cpp_property_get_flags(prop);

        public static IntPtr il2cpp_property_get_get_method(IntPtr prop) => UnityVersionHandler.propertyInfoStructHandler.il2cpp_property_get_get_method(prop);

        public static IntPtr il2cpp_property_get_set_method(IntPtr prop) => UnityVersionHandler.propertyInfoStructHandler.il2cpp_property_get_set_method(prop);

        public static IntPtr il2cpp_property_get_name(IntPtr prop) => UnityVersionHandler.propertyInfoStructHandler.il2cpp_property_get_name(prop);

        public static IntPtr il2cpp_property_get_parent(IntPtr prop) => UnityVersionHandler.propertyInfoStructHandler.il2cpp_property_get_parent(prop);

        #endregion

        #region Runtime Invoke

        public static unsafe IntPtr il2cpp_runtime_invoke(IntPtr method, IntPtr obj, void** param, ref IntPtr exc) => IL2CPP.il2cpp_runtime_invoke(method, obj, param, ref exc);

        // param can be of Il2CppObject*
        public static unsafe IntPtr il2cpp_runtime_invoke_convert_args(IntPtr method, IntPtr obj, void** param, int paramCount, ref IntPtr exc) => IL2CPP.il2cpp_runtime_invoke_convert_args(method, obj, param, paramCount, ref exc);

        public static void il2cpp_runtime_class_init(IntPtr klass) => IL2CPP.il2cpp_runtime_class_init(klass);

        public static void il2cpp_runtime_object_init(IntPtr obj) => IL2CPP.il2cpp_runtime_object_init(obj);

        public static void il2cpp_runtime_object_init_exception(IntPtr obj, ref IntPtr exc) => IL2CPP.il2cpp_runtime_object_init_exception(obj, ref exc);

        // public static void il2cpp_runtime_unhandled_exception_policy_set(IL2CPP_RuntimeUnhandledExceptionPolicy value) => IL2CPP.il2cpp_runtime_unhandled_exception_policy_set(value);

        #endregion

        #region Stats

        public static bool il2cpp_stats_dump_to_file(IntPtr path) => IL2CPP.il2cpp_stats_dump_to_file(path);

        //public static ulong il2cpp_stats_get_value(IL2CPP_Stat stat) => IL2CPP.il2cpp_stats_get_value(stat);

        #endregion

        #region Strings

        public static int il2cpp_string_length(IntPtr str) => IL2CPP.il2cpp_string_length(str);

        public static unsafe char* il2cpp_string_chars(IntPtr str) => IL2CPP.il2cpp_string_chars(str);

        public static IntPtr il2cpp_string_new(string str) => IL2CPP.il2cpp_string_new(str);

        public static IntPtr il2cpp_string_new_len(string str, uint length) => IL2CPP.il2cpp_string_new_len(str, length);

        public static IntPtr il2cpp_string_new_utf16(char* text, int len) => IL2CPP.il2cpp_string_new_utf16(text, len);

        public static IntPtr il2cpp_string_new_wrapper(string str) => IL2CPP.il2cpp_string_new_wrapper(str);

        public static IntPtr il2cpp_string_intern(string str) => IL2CPP.il2cpp_string_intern(str);

        public static IntPtr il2cpp_string_is_interned(string str) => IL2CPP.il2cpp_string_is_interned(str);

        #endregion

        #region Threads

        public static IntPtr il2cpp_thread_current() => IL2CPP.il2cpp_thread_current();

        public static IntPtr il2cpp_thread_attach(IntPtr domain) => IL2CPP.il2cpp_thread_attach(domain);

        public static void il2cpp_thread_detach(IntPtr thread) => IL2CPP.il2cpp_thread_detach(thread);

        public static void** il2cpp_thread_get_all_attached_threads(ref uint size) => IL2CPP.il2cpp_thread_get_all_attached_threads(ref size);

        public static bool il2cpp_is_vm_thread(IntPtr thread) => IL2CPP.il2cpp_is_vm_thread(thread);

        public static void il2cpp_current_thread_walk_frame_stack(IntPtr func, IntPtr user_data) => IL2CPP.il2cpp_current_thread_walk_frame_stack(func, user_data);

        public static void il2cpp_thread_walk_frame_stack(IntPtr thread, IntPtr func, IntPtr user_data) => IL2CPP.il2cpp_thread_walk_frame_stack(thread, func, user_data);

        public static bool il2cpp_current_thread_get_top_frame(IntPtr frame) => IL2CPP.il2cpp_current_thread_get_top_frame(frame);

        public static bool il2cpp_thread_get_top_frame(IntPtr thread, IntPtr frame) => IL2CPP.il2cpp_thread_get_top_frame(thread, frame);

        public static bool il2cpp_current_thread_get_frame_at(int offset, IntPtr frame) => IL2CPP.il2cpp_current_thread_get_frame_at(offset, frame);

        public static bool il2cpp_thread_get_frame_at(IntPtr thread, int offset, IntPtr frame) => IL2CPP.il2cpp_thread_get_frame_at(thread, offset, frame);

        public static int il2cpp_current_thread_get_stack_depth() => IL2CPP.il2cpp_current_thread_get_stack_depth();

        public static int il2cpp_thread_get_stack_depth(IntPtr thread) => IL2CPP.il2cpp_thread_get_stack_depth(thread);

        #endregion

        #region Types

        public static IntPtr il2cpp_type_get_object(IntPtr type) => IL2CPP.il2cpp_type_get_object(type);

        public static int il2cpp_type_get_type(IntPtr type) => IL2CPP.il2cpp_type_get_type(type);

        public static IntPtr il2cpp_type_get_class_or_element_class(IntPtr type) => IL2CPP.il2cpp_type_get_class_or_element_class(type);

        public static IntPtr il2cpp_type_get_name(IntPtr type) => IL2CPP.il2cpp_type_get_name(type);

        public static bool il2cpp_type_is_byref(IntPtr type) => IL2CPP.il2cpp_type_is_byref(type);

        public static uint il2cpp_type_get_attrs(IntPtr type) => IL2CPP.il2cpp_type_get_attrs(type);

        public static bool il2cpp_type_equals(IntPtr type, IntPtr otherType) => IL2CPP.il2cpp_type_equals(type, otherType);

        public static IntPtr il2cpp_type_get_assembly_qualified_name(IntPtr type) => IL2CPP.il2cpp_type_get_assembly_qualified_name(type);

        #endregion

        #region Unity Liveness

        public static IntPtr il2cpp_unity_liveness_calculation_begin(IntPtr filter, int max_object_count, IntPtr callback, IntPtr userdata, IntPtr onWorldStarted, IntPtr onWorldStopped) => IL2CPP.il2cpp_unity_liveness_calculation_begin(filter, max_object_count, callback, userdata, onWorldStarted, onWorldStopped);

        public static void il2cpp_unity_liveness_calculation_end(IntPtr state) => IL2CPP.il2cpp_unity_liveness_calculation_end(state);

        public static void il2cpp_unity_liveness_calculation_from_root(IntPtr root, IntPtr state) => IL2CPP.il2cpp_unity_liveness_calculation_from_root(root, state);

        public static void il2cpp_unity_liveness_calculation_from_statics(IntPtr state) => IL2CPP.il2cpp_unity_liveness_calculation_from_statics(state);

        #endregion
    }
}
