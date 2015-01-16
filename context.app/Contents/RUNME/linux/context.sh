#!/bin/sh

../Contents/Resources/Spoon\ 3\ beta\ 5\ processor.app/Contents/Linux/lib/spoon/4.4.7-2357/spoonvm ../Contents/Resources/Spoon\ 3\ beta\ 5\ processor.app/Contents/Resources/946BE974-48B7-4D11-B209-6355B3E49722.image &
sleep 5
xdg-open http://localhost:8090/README.html &
