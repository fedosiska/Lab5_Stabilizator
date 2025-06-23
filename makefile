CXX := g++
CXXFLAGS := -std=c++17 -O2 -Wall -Wextra -static-libgcc -static-libstdc++
LDFLAGS := -shared -Wl,--out-implib,libarm.a
SRC := arm_api.cpp
TARGET := libarm.dll


all: $(TARGET)

$(TARGET): $(SRC) arm_api.h
	$(CXX) $(CXXFLAGS) $(LDFLAGS) -o $@ $<


clean:
	del /Q $(TARGET) libarm.a 2>NUL || echo off


test:
	@if exist $(TARGET) ( \
		echo Библиотека $(TARGET) собрана успешно! &\
		dir $(TARGET) \
	) else ( \
		echo *** НЕ удалось собрать $(TARGET)! *** \
	)
