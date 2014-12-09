'From Squeak4.2 of 4 February 2011 [latest update: #10966] on 2 November 2013 at 3:36:42 pm'!!Interpreter methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 5/23/2011 12:12'!okayInterpreterObjects	| oopOrZero oop |	self okayFields: nilObj.	self okayFields: falseObj.	self okayFields: trueObj.	self okayFields: specialObjectsOop.	self okayFields: activeContext.	self okayFields: method.	self okayFields: receiver.	self okayFields: theHomeContext.	self okayFields: messageSelector.	self okayFields: newMethod.	self okayFields: lkupClass.	0 to: MethodCacheEntries - 1 by: MethodCacheEntrySize do: [ :i |		oopOrZero := methodCache at: i + MethodCacheSelector.		oopOrZero = 0 ifFalse: [			self okayFields: (methodCache at: i + MethodCacheSelector).			self okayFields: (methodCache at: i + MethodCacheClass).			self okayFields: (methodCache at: i + MethodCacheMethod).		].	].	0 to: MethodCacheEntries - 1 by: MethodCacheEntrySize do: [ :i |		oopOrZero := reportedMethodCache at: i + MethodCacheSelector.		oopOrZero = 0 ifFalse: [			self okayFields: (reportedMethodCache at: i + MethodCacheSelector).			self okayFields: (reportedMethodCache at: i + MethodCacheClass).		].	].	1 to: remapBufferCount do: [ :i |		oop := remapBuffer at: i.		(self isIntegerObject: oop) ifFalse: [			self okayFields: oop.		].	].	self okayActiveProcessStack.! !!Interpreter methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 5/22/2011 08:33'!initializeInterpreter: bytesToShift 	"Initialize Interpreter state before starting execution of a new image."	interpreterProxy := self sqGetInterpreterProxy.	self dummyReferToProxy.	self initializeObjectMemory: bytesToShift.	self initCompilerHooks.	activeContext := nilObj.	theHomeContext := nilObj.	method := nilObj.	receiver := nilObj.	messageSelector := nilObj.	newMethod := nilObj.	methodClass := nilObj.	lkupClass := nilObj.	receiverClass := nilObj.	newNativeMethod := nilObj.	self flushMethodCache.	self loadInitialContext.	self initialCleanup.	interruptCheckCounter := 0.	interruptCheckCounterFeedBackReset := 1000.	interruptChecksEveryNms := 1.	nextPollTick := 0.	nextWakeupTick := 0.	lastTick := 0.	interruptKeycode := 2094. "cmd-. as used for Mac but no other OS"	interruptPending := false.	semaphoresUseBufferA := true.	semaphoresToSignalCountA := 0.	semaphoresToSignalCountB := 0.	deferDisplayUpdates := false.	pendingFinalizationSignals := 0.	globalSessionID := 0.	[globalSessionID = 0]		whileTrue: [globalSessionID := self						cCode: 'time(NULL) + ioMSecs()'						inSmalltalk: [(Random new next * SmallInteger maxVal) asInteger]].	jmpDepth := 0.	jmpMax := MaxJumpBuf. "xxxx: Must match the definition of jmpBuf and suspendedCallbacks"	reportingSends := false.	lastMethodWasCached := false! !!Interpreter methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 10/28/2006 00:32'!commonSend	"Send a message, starting lookup with the receiver's class."	"Assume: messageSelector and argumentCount have been set, and that 	the receiver and arguments have been pushed onto the stack,"	"Note: This method is inlined into the interpreter dispatch loop."	self sharedCodeNamed: 'commonSend' inCase: 131.	self internalFindNewMethod.	self internalExecuteNewMethod.	self reportLastSend.	self fetchNextBytecode! !!Interpreter methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 5/22/2011 08:35'!internalFindNewMethod	"Find the compiled method to be run when the current messageSelector is sent to the class 'lkupClass', setting the values of 'newMethod' and 'primitiveIndex'."	<inline: true>	lastMethodWasCached := self lookupInMethodCacheSel: messageSelector class: lkupClass.	lastMethodWasCached ifFalse: [		"entry was not found in the cache; look it up the hard way"		self externalizeIPandSP.		self lookupMethodInClass: lkupClass.		self internalizeIPandSP.		self addNewMethodToCache].! !!Interpreter methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 5/23/2011 12:39'!reportLastSend	<inline: true>	reportingSends		ifTrue: [			((messageSelector = (self splObj: SelectorUnreportedWait)) or: [(messageSelector = (self splObj: SelectorUnreportedSignal))]) ifFalse: [				(messageSelector = (self splObj: SelectorStopReportingSends))					ifTrue: [reportingSends := false]					ifFalse: [						(self lookupInReportedMethodCacheSel: messageSelector class: lkupClass) ifFalse: [							"The method just run may not have been reported, and the active process is being imprinted."							|								behaviorIDAddress								classAddress								isMeta								metaByte								metaclassAddress								methodIDAddress								methodIDIndex								selectorSize							|							self addNewMethodToReportedCache.							lastMethodWasCached								ifTrue: [classAddress := lkupClass]								ifFalse: [classAddress := methodClass].							isMeta := ((self fetchClassOf: classAddress) = (self fetchClassOf: (self fetchClassOf: (self fetchClassOf: nilObj)))).							"syntactic Slang concession"							methodIDAddress := self splObj: ReportedMethodID.							isMeta								ifTrue: [metaclassAddress := classAddress]								ifFalse: [metaclassAddress := self fetchClassOf: classAddress].							behaviorIDAddress := (								self									fetchPointer: 6									ofObject: metaclassAddress).							"Write the behavior ID bytes."							methodIDIndex := 0.							0								to: 15								do: [:index |									self										storeByte: methodIDIndex										ofObject: methodIDAddress										withValue: (											self												fetchByte: index												ofObject: behaviorIDAddress).									methodIDIndex := methodIDIndex + 1].							"Write the message selector."							selectorSize := self stSizeOf: messageSelector.							self								storeByte: methodIDIndex								ofObject: methodIDAddress								withValue: selectorSize.							methodIDIndex := methodIDIndex + 1.							0								to: (selectorSize - 1)								do: [:index |									self										storeByte: methodIDIndex										ofObject: methodIDAddress										withValue: (											self												fetchByte: index												ofObject: messageSelector).									methodIDIndex := methodIDIndex + 1].							"Write the meta bit."							"syntactic Slang concession"							isMeta								ifTrue: [metaByte := 2r10000000]								ifFalse: [metaByte := 0].							self								storeByte: methodIDIndex								ofObject: methodIDAddress								withValue: metaByte.							"Reset the rest of the ID so that it can be used in cache lookup in the object memory."							methodIDIndex := methodIDIndex + 1.							methodIDIndex								to: 127								do: [:index |									self										storeByte: index										ofObject: methodIDAddress										withValue: 0].							self								externalizeIPandSP;								synchronousSignal: (self splObj: MethodToReport);								internalizeIPandSP]]]]		ifFalse: [			(messageSelector = (self splObj: SelectorStartReportingSends)) ifTrue: [reportingSends := true]]! !!Interpreter methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 8/21/2004 01:27'!addNewMethodToReportedCache	"Add the given entry to the reported-method cache.	The policy is as follows:		Look for an empty entry anywhere in the reprobe chain.		If found, install the new entry there.		If not found, then install the new entry at the first probe position			and delete the entries in the rest of the reprobe chain.		This has two useful purposes:			If there is active contention over the first slot, the second				or third will likely be free for reentry after ejection.			Also, flushing is good when reprobe chains are getting full."	| probe hash |	self inline: false.	self compilerTranslateMethodHook.	"newMethod x lkupClass -> newNativeMethod (may cause GC !!)"	hash := messageSelector bitXor: lkupClass.  "drop low-order zeros from addresses"	0 to: CacheProbeMax-1 do:		[:p | probe := (hash >> p) bitAnd: MethodCacheMask.		(reportedMethodCache at: probe + MethodCacheSelector) = 0 ifTrue:				["Found an empty entry -- use it"				reportedMethodCache at: probe + MethodCacheSelector put: messageSelector.				reportedMethodCache at: probe + MethodCacheClass put: lkupClass.				^ nil]].	"OK, we failed to find an entry -- install at the first slot..."	probe := hash bitAnd: MethodCacheMask.  "first probe"	reportedMethodCache at: probe + MethodCacheSelector put: messageSelector.	reportedMethodCache at: probe + MethodCacheClass put: lkupClass.	"...and zap the following entries"	1 to: CacheProbeMax-1 do:		[:p | probe := (hash >> p) bitAnd: MethodCacheMask.		reportedMethodCache at: probe + MethodCacheSelector put: 0].! !!Interpreter methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 5/22/2011 08:24'!flushMethodCache	"Flush the method cache. The method cache is flushed on every programming change and garbage collect."	1 to: MethodCacheSize do: [ :i | methodCache at: i put: 0 ].	1 to: MethodCacheSize do: [ :i | reportedMethodCache at: i put: 0 ].	self flushAtCache! !!Interpreter methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 5/22/2011 08:26'!flushMethodCacheFrom: memStart to: memEnd 	"Flush entries in the method cache only if the oop address is within the given memory range. 	This reduces overagressive cache clearing. Note the AtCache is fully flushed, 70% of the time 	cache entries live in newspace, new objects die young"	| probe |	probe := 0.	1 to: MethodCacheEntries do: [:i | 			(methodCache at: probe + MethodCacheSelector) = 0				ifFalse: [(((((self oop: (methodCache at: probe + MethodCacheSelector) isGreaterThanOrEqualTo: memStart)										and: [self oop: (methodCache at: probe + MethodCacheSelector) isLessThan: memEnd])									or: [(self oop: (methodCache at: probe + MethodCacheClass) isGreaterThanOrEqualTo: memStart)											and: [self oop: (methodCache at: probe + MethodCacheClass) isLessThan: memEnd]])								or: [(self oop: (methodCache at: probe + MethodCacheMethod) isGreaterThanOrEqualTo: memStart)										and: [self oop: (methodCache at: probe + MethodCacheMethod) isLessThan: memEnd]])							or: [(self oop: (methodCache at: probe + MethodCacheNative) isGreaterThanOrEqualTo: memStart)									and: [self oop: (methodCache at: probe + MethodCacheNative) isLessThan: memEnd]])						ifTrue: [methodCache at: probe + MethodCacheSelector put: 0]].			probe := probe + MethodCacheEntrySize].			probe := 0.	1 to: MethodCacheEntries do: [:i | 			(reportedMethodCache at: probe + MethodCacheSelector) = 0				ifFalse: [(((((self oop: (reportedMethodCache at: probe + MethodCacheSelector) isGreaterThanOrEqualTo: memStart)										and: [self oop: (reportedMethodCache at: probe + MethodCacheSelector) isLessThan: memEnd])									or: [(self oop: (reportedMethodCache at: probe + MethodCacheClass) isGreaterThanOrEqualTo: memStart)											and: [self oop: (reportedMethodCache at: probe + MethodCacheClass) isLessThan: memEnd]])								or: [(self oop: (reportedMethodCache at: probe + MethodCacheMethod) isGreaterThanOrEqualTo: memStart)										and: [self oop: (reportedMethodCache at: probe + MethodCacheMethod) isLessThan: memEnd]])							or: [(self oop: (reportedMethodCache at: probe + MethodCacheNative) isGreaterThanOrEqualTo: memStart)									and: [self oop: (reportedMethodCache at: probe + MethodCacheNative) isLessThan: memEnd]])						ifTrue: [reportedMethodCache at: probe + MethodCacheSelector put: 0]].			probe := probe + MethodCacheEntrySize].	1 to: AtCacheTotalSize do: [:i | atCache at: i put: 0]! !!Interpreter methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 5/22/2011 08:38'!lookupInMethodCacheSel: selector class: class	"This method implements a simple method lookup cache. If an entry for the given selector and class is found in the cache, set the values of 'newMethod' and 'primitiveIndex' and return true. Otherwise, return false."	"About the re-probe scheme: The hash is the low bits of the XOR of two large addresses, minus their useless lowest two bits. If a probe doesn't get a hit, the hash is shifted right one bit to compute the next probe, introducing a new randomish bit. The cache is probed CacheProbeMax times before giving up."	"WARNING: Since the hash computation is based on the object addresses of the class and selector, we must rehash or flush when compacting storage. We've chosen to flush, since that also saves the trouble of updating the addresses of the objects in the cache."	| hash probe |	<inline: true>		(class = (self splObj: ClassProxy)) ifTrue: [		(selector ~= (self splObj: SelectorForward)) ifTrue: [^false]].		hash := selector bitXor: class.  "shift drops two low-order zeros from addresses"	probe := hash bitAnd: MethodCacheMask.  "first probe"	(((methodCache at: probe + MethodCacheSelector) = selector) and:		 [(methodCache at: probe + MethodCacheClass) = class]) ifTrue:			[newMethod := methodCache at: probe + MethodCacheMethod.			primitiveIndex := methodCache at: probe + MethodCachePrim.			newNativeMethod := methodCache at: probe + MethodCacheNative.			primitiveFunctionPointer := self cCoerce: (methodCache at: probe + MethodCachePrimFunction) to: 'void *'.			^ true	"found entry in cache; done"].	probe := (hash >> 1) bitAnd: MethodCacheMask.  "second probe"	(((methodCache at: probe + MethodCacheSelector) = selector) and:		 [(methodCache at: probe + MethodCacheClass) = class]) ifTrue:			[newMethod := methodCache at: probe + MethodCacheMethod.			primitiveIndex := methodCache at: probe + MethodCachePrim.			newNativeMethod := methodCache at: probe + MethodCacheNative.			primitiveFunctionPointer := self cCoerce: (methodCache at: probe + MethodCachePrimFunction) to: 'void *'.			^ true	"found entry in cache; done"].	probe := (hash >> 2) bitAnd: MethodCacheMask.	(((methodCache at: probe + MethodCacheSelector) = selector) and:		 [(methodCache at: probe + MethodCacheClass) = class]) ifTrue:			[newMethod := methodCache at: probe + MethodCacheMethod.			primitiveIndex := methodCache at: probe + MethodCachePrim.			newNativeMethod := methodCache at: probe + MethodCacheNative.			primitiveFunctionPointer := self cCoerce: (methodCache at: probe + MethodCachePrimFunction) to: 'void *'.			^ true	"found entry in cache; done"].	^ false! !!Interpreter methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 5/22/2011 08:41'!lookupInReportedMethodCacheSel: selector class: class	"This method implements a simple method lookup cache. If an entry for the given selector and class is found in the cache, set the values of 'newMethod' and 'primitiveIndex' and return true. Otherwise, return false."	"About the re-probe scheme: The hash is the low bits of the XOR of two large addresses, minus their useless lowest two bits. If a probe doesn't get a hit, the hash is shifted right one bit to compute the next probe, introducing a new randomish bit. The cache is probed CacheProbeMax times before giving up."	"WARNING: Since the hash computation is based on the object addresses of the class and selector, we must rehash or flush when compacting storage. We've chosen to flush, since that also saves the trouble of updating the addresses of the objects in the cache."	| hash probe |	<inline: true>		hash := selector bitXor: class.  "shift drops two low-order zeros from addresses"	probe := hash bitAnd: MethodCacheMask.  "first probe"	(((reportedMethodCache at: probe + MethodCacheSelector) = selector) and:		 [(reportedMethodCache at: probe + MethodCacheClass) = class]) ifTrue:			[newMethod := reportedMethodCache at: probe + MethodCacheMethod.			primitiveIndex := reportedMethodCache at: probe + MethodCachePrim.			newNativeMethod := reportedMethodCache at: probe + MethodCacheNative.			primitiveFunctionPointer := self cCoerce: (reportedMethodCache at: probe + MethodCachePrimFunction) to: 'void *'.			^ true	"found entry in cache; done"].	probe := (hash >> 1) bitAnd: MethodCacheMask.  "second probe"	(((reportedMethodCache at: probe + MethodCacheSelector) = selector) and:		 [(reportedMethodCache at: probe + MethodCacheClass) = class]) ifTrue:			[newMethod := reportedMethodCache at: probe + MethodCacheMethod.			primitiveIndex := reportedMethodCache at: probe + MethodCachePrim.			newNativeMethod := reportedMethodCache at: probe + MethodCacheNative.			primitiveFunctionPointer := self cCoerce: (reportedMethodCache at: probe + MethodCachePrimFunction) to: 'void *'.			^ true	"found entry in cache; done"].	probe := (hash >> 2) bitAnd: MethodCacheMask.	(((reportedMethodCache at: probe + MethodCacheSelector) = selector) and:		 [(reportedMethodCache at: probe + MethodCacheClass) = class]) ifTrue:			[newMethod := reportedMethodCache at: probe + MethodCacheMethod.			primitiveIndex := reportedMethodCache at: probe + MethodCachePrim.			newNativeMethod := reportedMethodCache at: probe + MethodCacheNative.			primitiveFunctionPointer := self cCoerce: (reportedMethodCache at: probe + MethodCachePrimFunction) to: 'void *'.			^ true	"found entry in cache; done"].	^ false! !!Interpreter methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 5/23/2011 12:43'!transferTo: aProc 	"Record a process to be awoken on the next interpreter cycle. 	ikp 11/24/1999 06:07 -- added hook for external runtime 	compiler "	| sched oldProc newProc |	newProc := aProc.	sched := self schedulerPointer.	oldProc := self fetchPointer: ActiveProcessIndex ofObject: sched.	self storePointer: ActiveProcessIndex ofObject: sched withValue: newProc.	compilerInitialized		ifTrue: [self compilerProcessChange: oldProc to: newProc]		ifFalse: [self storePointer: SuspendedContextIndex ofObject: oldProc withValue: activeContext.			self newActiveContext: (self fetchPointer: SuspendedContextIndex ofObject: newProc).			self storePointer: SuspendedContextIndex ofObject: newProc withValue: nilObj.						(				(					self						fetchPointer: BeingImprintedIndex						ofObject: (							self								fetchPointer: ActiveProcessIndex								ofObject: self schedulerPointer)				)					== trueObj			)				ifTrue: [reportingSends := true]				ifFalse: [reportingSends := false]].	reclaimableContextCount := 0! !!InterpreterSimulator methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 5/23/2011 16:27'!initialize	"Initialize the InterpreterSimulator when running the interpreter inside	Smalltalk. The primary responsibility of this method is to allocate	Smalltalk Arrays for variables that will be declared as statically-allocated	global arrays in the translated code."	"initialize class variables"	ObjectMemory initializeConstants.	Interpreter initialize.	"Note: we must initialize ConstMinusOne differently for simulation,		due to the fact that the simulator works only with +ve 32-bit values"	ConstMinusOne := self integerObjectOf: -1.	methodCache := Array new: MethodCacheSize.	reportedMethodCache := Array new: MethodCacheSize.	atCache := Array new: AtCacheTotalSize.	self flushMethodCache.	rootTable := Array new: RootTableSize.	weakRoots := Array new: RootTableSize + RemapBufferSize + 100.	remapBuffer := Array new: RemapBufferSize.	semaphoresUseBufferA := true.	semaphoresToSignalA := Array new: SemaphoresToSignalSize.	semaphoresToSignalB := Array new: SemaphoresToSignalSize.	externalPrimitiveTable := CArrayAccessor on: (Array new: MaxExternalPrimitiveTableSize).	primitiveTable := self class primitiveTable.	pluginList := #().	mappedPluginEntries := #().	"initialize InterpreterSimulator variables used for debugging"	byteCount := 0.	sendCount := 0.	quitBlock := [^ self].	traceOn := true.	myBitBlt := BitBltSimulator new setInterpreter: self.	filesOpen := OrderedCollection new.	headerTypeBytes := CArrayAccessor on: (Array with: self bytesPerWord * 2 with: self bytesPerWord with: 0 with: 0).	transcript := Transcript.	displayForm := 'Display has not yet been installed' asDisplayText form.	! !!ObjectMemory class methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 7/18/2011 22:37'!initializeSpecialObjectIndices	"Initialize indices into specialObjects array."	NilObject := 0.	FalseObject := 1.	TrueObject := 2.	SchedulerAssociation := 3.	ClassBitmap := 4.	ClassInteger := 5.	ClassString := 6.	ClassArray := 7.	"SmalltalkDictionary := 8."  "Do not delete!!"	ClassFloat := 9.	ClassMethodContext := 10.	ClassBlockContext := 11.	ClassPoint := 12.	ClassLargePositiveInteger := 13.	TheDisplay := 14.	ClassMessage := 15.	ClassCompiledMethod := 16.	TheLowSpaceSemaphore := 17.	ClassSemaphore := 18.	ClassCharacter := 19.	SelectorDoesNotUnderstand := 20.	SelectorCannotReturn := 21.	ProcessSignalingLowSpace := 22.	"was TheInputSemaphore"	SpecialSelectors := 23.	CharacterTable := 24.	SelectorMustBeBoolean := 25.	ClassByteArray := 26.	ClassProcess := 27.	CompactClasses := 28.	TheTimerSemaphore := 29.	TheInterruptSemaphore := 30.	SelectorCannotInterpret := 34.	"Was MethodContextProto := 35."	ClassBlockClosure := 36.	"Was BlockContextProto := 37."	ExternalObjectsArray := 38.	ClassPseudoContext := 39.	ClassTranslatedMethod := 40.	TheFinalizationSemaphore := 41.	ClassLargeNegativeInteger := 42.	ClassExternalAddress := 43.	ClassExternalStructure := 44.	ClassExternalData := 45.	ClassExternalFunction := 46.	ClassExternalLibrary := 47.	SelectorAboutToReturn := 48.	SelectorRunWithIn := 49.	SelectorAttemptToAssign := 50.	"PrimErrTableIndex := 51. in Interpreter class>>initializePrimitiveErrorCodes"	ClassAlien := 52.	InvokeCallbackSelector := 53.	ClassUnsafeAlien := 54.	ClassWeakFinalizer := 55.		ClassProxy := 107.	SelectorForward := 108.	SelectorInitProxy := 109.	SelectorProxyHash := 110.	SelectorCounterpart := 111.	SelectorIsNil := 112.	SelectorNextInstance := 113.	SelectorStoreOnProxyStream := 114.	SelectorStartReportingSends := 115.	ReportedMethodID := 116.	MethodToReport := 117.	SelectorStopReportingSends := 118.	SelectorUnreportedWait := 119.	SelectorUnreportedSignal := 120! !!Interpreter class methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 5/23/2011 12:44'!declareCVarsIn: aCCodeGenerator	aCCodeGenerator addHeaderFile:'<setjmp.h>'.	aCCodeGenerator 		var: #interpreterProxy 		type: #'struct VirtualMachine*'.	aCCodeGenerator		var: #primitiveTable		declareC: 'void *primitiveTable[', (MaxPrimitiveIndex +2) printString, '] = ',	self primitiveTableString.	aCCodeGenerator		var: #primitiveFunctionPointer		declareC: 'void *primitiveFunctionPointer'				.		"xxxx FIX THIS STUPIDITY xxxx - ikp. What he means is use a better type than void *, apparently - tpr"	aCCodeGenerator		var: #methodCache		declareC: 'long methodCache[', (MethodCacheSize + 1) printString, ']'.	aCCodeGenerator		var: #reportedMethodCache		declareC: 'long reportedMethodCache[', (MethodCacheSize + 1) printString, ']'.	aCCodeGenerator		var: #atCache		declareC: 'sqInt atCache[', (AtCacheTotalSize + 1) printString, ']'.	aCCodeGenerator var: #statGCTime type: #'sqLong'.	aCCodeGenerator var: #statFullGCMSecs type: #'sqLong'.	aCCodeGenerator var: #statIGCDeltaTime type: #'sqLong'.	aCCodeGenerator var: #statIncrGCMSecs type: #'sqLong'.	aCCodeGenerator var: #localIP type: #'char*'.	aCCodeGenerator var: #localSP type: #'char*'.	aCCodeGenerator var: #showSurfaceFn type: #'void*'.	aCCodeGenerator var: 'semaphoresToSignalA'		declareC: 'sqInt semaphoresToSignalA[', (SemaphoresToSignalSize + 1) printString, ']'.	aCCodeGenerator var: 'semaphoresToSignalB'		declareC: 'sqInt semaphoresToSignalB[', (SemaphoresToSignalSize + 1) printString, ']'.	aCCodeGenerator		var: #compilerHooks		declareC: 'sqInt (*compilerHooks[', (CompilerHooksSize + 1) printString, '])()'.	aCCodeGenerator		var: #interpreterVersion		declareC: 'const char *interpreterVersion = "', SmalltalkImage current datedVersion, ' [', SmalltalkImage current lastUpdateString,']"'.	aCCodeGenerator		var: #obsoleteIndexedPrimitiveTable		declareC: 'char* obsoleteIndexedPrimitiveTable[][3] = ', self obsoleteIndexedPrimitiveTableString.	aCCodeGenerator		var: #obsoleteNamedPrimitiveTable		declareC: 'const char* obsoleteNamedPrimitiveTable[][3] = ', self obsoleteNamedPrimitiveTableString.	aCCodeGenerator		var: #externalPrimitiveTable		declareC: 'void *externalPrimitiveTable[', (MaxExternalPrimitiveTableSize + 1) printString, ']'.	self declareCAsOop: {			#instructionPointer .			#method .			#newMethod .			#activeContext .			#theHomeContext .			#stackPointer }		in: aCCodeGenerator.			aCCodeGenerator		var: #jmpBuf		declareC: 'jmp_buf jmpBuf[', (MaxJumpBuf + 1) printString, ']'.	aCCodeGenerator		var: #suspendedCallbacks		declareC: 'sqInt suspendedCallbacks[', (MaxJumpBuf + 1) printString, ']'.	aCCodeGenerator		var: #suspendedMethods		declareC: 'sqInt suspendedMethods[', (MaxJumpBuf + 1) printString, ']'.	"Reinitialized at interpreter entry by #initializeImageFormatVersion"	aCCodeGenerator		var: #imageFormatVersionNumber		declareC: 'sqInt imageFormatVersionNumber = 0'.	"Declared here to prevent inclusion in foo struct by CCodeGeneratorGlobalStructure"	aCCodeGenerator		var: #imageFormatInitialVersion		declareC: 'sqInt imageFormatInitialVersion = 0'! !!Interpreter class methodsFor: '*System-Spoon-Virtual Machine Support' stamp: 'crl 5/23/2011 12:48'!initializeSchedulerIndices	"Class ProcessorScheduler"	ProcessListsIndex := 0.	ActiveProcessIndex := 1.	"Class LinkedList"	FirstLinkIndex := 0.	LastLinkIndex := 1.	"Class Semaphore"	ExcessSignalsIndex := 2.	"Class Link"	NextLinkIndex := 0.	"Class Process"	SuspendedContextIndex := 1.	PriorityIndex := 2.	MyListIndex := 3.	BeingImprintedIndex := 6! !