OUT_DIR = ../../../out/shared/gcm
CC = gcc
CFLAGS = -g -Wall #-O2
EXEC_FILE = $(OUT_DIR)/git-credential-manager
SRC_FILES = git-credential-manager.c

.PHONY: all clean

all: $(EXEC_FILE)

$(EXEC_FILE): $(SRC_FILES)
	mkdir -p $(OUT_DIR)
	$(CC) -o $@ $(CFLAGS) $^

clean:
	rm -f $(EXEC_FILE)
