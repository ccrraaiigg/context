/*
 * Flow.js: networking plugin for the SqueakJS virtual machine.
 *
 */
 
function Flow() {
    var interpreterProxy,
        primHandler;

    function setInterpreter(anInterpreterProxy) {
        // Slang interface
        interpreterProxy = anInterpreterProxy;
        // PrimHandler methods for convenience
        primHandler = interpreterProxy.vm.primHandler;
        // success
        return true;};

    function enableResolver(resolverHandle) {
	if (argCount !== 1) return false; // fail
	var resolverHandle = interpreterProxy.stackObjectValue(0);
        if (interpreterProxy.failed()) return false; // fail
	interpreterProxy.storePointerOfObjectwithValue(



        var result = examplePrimitiveHelperFunction(which);
        if (!result) return false; // fail
        var resultObj = primHandler.makeStString(result);
        interpreterProxy.popthenPush(1 + argCount, resultObj);
        return true; // success};

    // hide private functions
    return {
        setInterpreter: setInterpreter,
	examplePrimitive: examplePrimitiveInfo,}};

// register plugin in global Squeak object
window.addEventListener("load", function() {
	Squeak.registerExternalModule('Flow', Flow());});

