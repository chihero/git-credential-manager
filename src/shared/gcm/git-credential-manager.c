#include <errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <limits.h>
#include <pwd.h>

#define GCM_DIRNAME ".gcm"
#define GCM_PIPENAME ".pipe"
#define GCM_DAEMONNAME "gcmd"
#define GCM_DAEMONPATH "./gcmd"

static void die_err(const char *msg, int err)
{
	fprintf(stderr, "fatal: %s (0x%x)\n", msg, err);
	exit(err);
}

static void die_errno(const char *msg)
{
	int err = errno;
	fprintf(stderr, "fatal: %s (%s: 0x%x)\n", msg, strerror(err), err);
	exit(err);
}

static void die(const char *msg)
{
	die_err(msg, 1);
}

static void usage(const char *cmdline)
{
	fprintf(stderr, "usage: %s\n", cmdline);
	exit(127);
}

static uid_t gcm_ruid(void)
{
	uid_t uid;
	char *sudo_uid;
	char *end;

	uid = getuid();
	if (uid == 0) {
		// we are root so try and read SUDO_UID environment variable instead
		sudo_uid = getenv("SUDO_UID");
		if (!sudo_uid)
			die("was unable to read SUDO_UID and cannot run directly as root");

		// convert string to long
		errno = 0;
		uid = strtol(sudo_uid, &end, 10);
		if (end == sudo_uid || (unsigned long)uid == ULONG_MAX || errno == ERANGE)
			die_errno("was unable to parse SUDO_UID as an integer");
	}

	return uid;
}

static char *gcm_sockpath(uid_t uid)
{
	struct passwd *pw = getpwuid(uid);
	if (!pw)
		die("unable to get user information");

	const char *homedir = pw->pw_dir;
	if (!homedir)
		die("unable to get user home directory");

	char *sockpath = malloc(strlen(homedir) + strlen(GCM_DIRNAME) +
							strlen(GCM_PIPENAME) + 3);
	if (!sockpath)
		die("unable to allocate memory");

	sprintf(sockpath, "%s/%s/%s", homedir, GCM_DIRNAME, GCM_PIPENAME);

	return sockpath;
}

int main(int argc, char **argv)
{
	int fd;
	char buf[1024];
	struct sockaddr_un addr = {0};
	socklen_t addrlen = sizeof(addr);
	const char *trace_str = getenv("GCM_TRACE");
	int trace = trace_str && !strcmp(trace_str, "1");
	char *sockpath = gcm_sockpath(gcm_ruid());

	if (argc < 2)
		usage("client <command>");

	if (trace)
		fprintf(stderr, "connecting to %s\n", sockpath);

	fd = socket(AF_UNIX, SOCK_STREAM, 0);
	if (fd < 0)
		die_errno("socket");

	addr.sun_family = AF_UNIX;
	strcpy(addr.sun_path, sockpath);
	if (connect(fd, (struct sockaddr *)&addr, addrlen) < 0) {
		if (trace)
			fprintf(stderr, "starting daemon");

		if (!fork()) {
			execl(GCM_DAEMONPATH, GCM_DAEMONNAME, NULL);
			die_errno("execlp");
		}
		sleep(1);

		// try connecting again
		if (connect(fd, (struct sockaddr *)&addr, addrlen) < 0)
			die_errno("connect");
	}

	// send primary argument first
	snprintf(buf, sizeof(buf), "%s\n", argv[1]);
	if (send(fd, buf, strlen(buf), 0) < 0)
		die_errno("send");

	// stream stdin to socket
	memset(buf, 0, sizeof(buf));
	while (fgets(buf, sizeof(buf), stdin)) {
		if (send(fd, buf, strlen(buf), 0) < 0)
			die_errno("send");

		if (strcmp(buf, "\n") == 0)
			break;
	}

	// read from socket to stdout
	memset(buf, 0, sizeof(buf));
	while (recv(fd, buf, sizeof(buf), 0) > 0) {
		fputs(buf, stdout);
	}

	// close
	close(fd);
	free(sockpath);

	return 0;
}
